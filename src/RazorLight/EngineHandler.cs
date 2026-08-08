using System;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.DependencyInjection;
using RazorLight.Caching;
using RazorLight.Compilation;
using RazorLight.DependencyInjection;
using RazorLight.Internal.Buffering;

namespace RazorLight
{
	internal sealed class EngineHandler : IEngineHandler, IDisposable
	{
		private readonly ITemplateCompilerCache? _compilerCache;
		private IServiceScopeFactory? _scopeFactory;
		private PropertyInjector? _propertyInjector;
		private readonly IDisposable? _ownedProject;
		private readonly IDisposable? _ownedCachingProvider;
		private static readonly ConditionalWeakTable<ITemplatePage, PageRenderState> PageRenderStates = new();
		private int _disposed;

		public EngineHandler(
			RazorLightOptions options,
			IRazorTemplateCompiler compiler,
			ITemplateFactoryProvider factoryProvider,
			ICachingProvider? cache,
			IDisposable? ownedProject = null,
			IDisposable? ownedCachingProvider = null)
		{
			Options = options ?? throw new ArgumentNullException(nameof(options));
			Compiler = compiler ?? throw new ArgumentNullException(nameof(compiler));
			FactoryProvider = factoryProvider ?? throw new ArgumentNullException(nameof(factoryProvider));
			_ownedProject = ownedProject;
			_ownedCachingProvider = ownedCachingProvider;

			_compilerCache = compiler is RazorTemplateCompiler razorTemplateCompiler
				? new RazorTemplateCompilerCache(razorTemplateCompiler)
				: null;
			Cache = cache != null && _compilerCache != null
				? new CoordinatedCachingProvider(cache, _compilerCache)
				: cache;
			Options.CachingProvider = Cache;
		}

		internal void ConfigureServices(IServiceScopeFactory scopeFactory, PropertyInjector propertyInjector)
		{
			_scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
			_propertyInjector = propertyInjector ?? throw new ArgumentNullException(nameof(propertyInjector));
		}

		public RazorLightOptions Options { get; }
		public ICachingProvider? Cache { get; }
		public IRazorTemplateCompiler Compiler { get; }
		public ITemplateFactoryProvider FactoryProvider { get; }
		internal IDisposable? OwnedCachingProvider => _ownedCachingProvider;

		[MemberNotNullWhen(true, nameof(Cache))]
		public bool IsCachingEnabled => Cache != null;

		public void Dispose()
		{
			if (Interlocked.Exchange(ref _disposed, 1) != 0)
			{
				return;
			}

			(Compiler as IDisposable)?.Dispose();
			_ownedCachingProvider?.Dispose();
			if (!ReferenceEquals(_ownedProject, _ownedCachingProvider))
			{
				_ownedProject?.Dispose();
			}
		}

		/// <summary>
		/// Search and compile a template with a given key
		/// </summary>
		/// <param name="key">Unique key of the template</param>
		/// <returns>An instance of a template</returns>
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public async Task<ITemplatePage> CompileTemplateAsync(string key)
			=> await CompileTemplateAsync(key, CancellationToken.None).ConfigureAwait(false);

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public async Task<ITemplatePage> CompileTemplateAsync(string key, CancellationToken cancellationToken)
		{
			return await CompileTemplateAsync(TemplateCompilationRequest.ForProject(
				NormalizeProjectKey(key),
				modelType: null,
				Options.Namespaces), cancellationToken).ConfigureAwait(false);
		}

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		private async Task<ITemplatePage> CompileTemplateAsync(
			TemplateCompilationRequest request,
			CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			InvalidatePreviousStringTemplate(request);
			var coordinatedCache = Cache as ICoordinatedCachingProvider;
			long cacheVersion = coordinatedCache?.BeginCompilation(request.TemplateKey) ?? 0;

			try
			{
			ITemplatePage? templatePage = null;
			if (Cache != null)
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (Cache.TryGetTemplate(request.CacheKey, out Func<ITemplatePage>? pageFactory))
				{
					templatePage = pageFactory();
				}
			}

			if (templatePage == null)
			{
				cancellationToken.ThrowIfCancellationRequested();
				CompiledTemplateDescriptor templateDescriptor;
				if (request.TemplateContent != null)
				{
					templateDescriptor = await Compiler.CompileAsync(
						request.TemplateKey,
						request.TemplateContent,
						request.ModelType,
						cancellationToken).ConfigureAwait(false);
				}
				else if (request.ModelType != null)
				{
					templateDescriptor = await Compiler.CompileAsync(request.TemplateKey, request.ModelType, cancellationToken).ConfigureAwait(false);
				}
				else
				{
					templateDescriptor = await Compiler.CompileAsync(request.TemplateKey, cancellationToken).ConfigureAwait(false);
				}

				Func<ITemplatePage> templateFactory = FactoryProvider.CreateFactory(templateDescriptor);

				if (Cache != null)
				{
					StoreCompiledTemplate(
						request.TemplateKey,
						request.CacheKey,
						templateFactory,
						templateDescriptor.ExpirationToken,
						cacheVersion);

					if (request.IsStringTemplate)
					{
						StoreCompiledTemplate(
							request.TemplateKey,
							request.TemplateKey,
							templateFactory,
							templateDescriptor.ExpirationToken,
							cacheVersion);
					}
				}

				templatePage = templateFactory();
			}

			templatePage.OutputEncoder = Options.OutputEncoder;
			return templatePage;
			}
			finally
			{
				coordinatedCache?.CompleteCompilation(request.TemplateKey);
			}
		}

		/// <summary>
		/// Renders a template with a given model
		/// </summary>
		/// <param name="templatePage">Instance of a template</param>
		/// <param name="model">Template model</param>
		/// <param name="viewBag">Dynamic viewBag of the template</param>
		/// <returns>Rendered string</returns>
		public async Task<string> RenderTemplateAsync<T>(ITemplatePage templatePage, T model, ExpandoObject? viewBag = null)
			=> await RenderTemplateAsync(templatePage, model, viewBag, CancellationToken.None).ConfigureAwait(false);

		public async Task<string> RenderTemplateAsync<T>(
			ITemplatePage templatePage,
			T model,
			ExpandoObject? viewBag,
			CancellationToken cancellationToken)
		{
			using (var writer = new StringWriter())
			{
				await RenderTemplateAsync(templatePage, model, writer, viewBag, cancellationToken).ConfigureAwait(false);

				return writer.ToString();
			}
		}

		/// <summary>
		/// Renders a template to the specified <paramref name="textWriter"/>
		/// </summary>
		/// <param name="templatePage">Instance of a template</param>
		/// <param name="model">Template model</param>
		/// <param name="viewBag">Dynamic viewBag of the page</param>
		/// <param name="textWriter">Output</param>
		public async Task RenderTemplateAsync<T>(
			ITemplatePage templatePage,
			T model,
			TextWriter textWriter,
			ExpandoObject? viewBag = null) =>
			await RenderTemplateAsync(templatePage, model, textWriter, viewBag, CancellationToken.None).ConfigureAwait(false);

		public async Task RenderTemplateAsync<T>(
			ITemplatePage templatePage,
			T model,
			TextWriter textWriter,
			ExpandoObject? viewBag,
			CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			using PageRenderLease renderLease = BeginRender(templatePage);
			SetModelContext(templatePage, textWriter, model, viewBag, cancellationToken);

			using (var bufferScope = new MemoryPoolViewBufferScope())
			{
				if (_scopeFactory == null)
				{
					var renderer = new TemplateRenderer(this, bufferScope, InitializePage);
					await renderer.RenderAsync(templatePage, cancellationToken).ConfigureAwait(false);
					return;
				}

				await using (AsyncServiceScope renderScope = _scopeFactory.CreateAsyncScope())
				{
					var renderer = new TemplateRenderer(
						this,
						bufferScope,
						page => InitializePage(page, renderScope.ServiceProvider));
					await renderer.RenderAsync(templatePage, cancellationToken).ConfigureAwait(false);
				}
			}
		}

		private void InitializePage(ITemplatePage page)
		{
			InitializePage(page, services: null);
		}

		private void InitializePage(ITemplatePage page, IServiceProvider? services)
		{
			foreach (Action<ITemplatePage> initializer in Options.PageInitializers)
			{
				initializer(page);
			}

			if (services != null)
			{
				_propertyInjector!.Inject(page, services);
			}
		}

		public async Task RenderIncludedTemplateAsync<T>(
			ITemplatePage templatePage,
			T model,
			TextWriter textWriter,
			ExpandoObject? viewBag,
			TemplateRenderer templateRenderer,
			CancellationToken cancellationToken)
		{
			SetModelContext(templatePage, textWriter, model, viewBag, cancellationToken);
			await templateRenderer.RenderAsync(templatePage, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Compiles and renders a template with a given <paramref name="key"/>
		/// </summary>
		/// <param name="key">Unique key of the template</param>
		/// <param name="model">Template model</param>
		/// <param name="viewBag">Dynamic ViewBag (can be null)</param>
		/// <returns></returns>
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public async Task<string> CompileRenderAsync<T>(string key, T model, ExpandoObject? viewBag = null)
			=> await CompileRenderAsync(key, model, viewBag, CancellationToken.None).ConfigureAwait(false);

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public async Task<string> CompileRenderAsync<T>(string key, T model, ExpandoObject? viewBag, CancellationToken cancellationToken)
		{
			ITemplatePage template = await CompileTemplateAsync(key, cancellationToken).ConfigureAwait(false);

			return await RenderTemplateAsync(template, model, viewBag, cancellationToken).ConfigureAwait(false);
		}

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public async Task<string> CompileRenderAsync(
			string key,
			object? model,
			Type modelType,
			ExpandoObject? viewBag = null) =>
			await CompileRenderAsync(key, model, modelType, viewBag, CancellationToken.None).ConfigureAwait(false);

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public async Task<string> CompileRenderAsync(
			string key,
			object? model,
			Type modelType,
			ExpandoObject? viewBag,
			CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (modelType == null)
			{
				throw new ArgumentNullException(nameof(modelType));
			}

			ITemplatePage template = await CompileTemplateAsync(TemplateCompilationRequest.ForProject(
				NormalizeProjectKey(key),
				modelType,
				Options.Namespaces), cancellationToken).ConfigureAwait(false);

			return await RenderTemplateAsync(template, model, modelType, viewBag, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Compiles and renders a template. Template content is taken directly from <paramref name="content"/> parameter
		/// </summary>
		/// <param name="key">Unique key of the template</param>
		/// <param name="content">Content of the template</param>
		/// <param name="model">Template model</param>
		/// <param name="viewBag">Dynamic ViewBag</param>
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Task<string> CompileRenderStringAsync<T>(
			string key,
			string content,
			T model,
			ExpandoObject? viewBag = null) =>
			CompileRenderStringAsync(key, content, model, viewBag, CancellationToken.None);

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Task<string> CompileRenderStringAsync<T>(
			string key,
			string content,
			T model,
			ExpandoObject? viewBag,
			CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (string.IsNullOrEmpty(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			if (string.IsNullOrEmpty(content))
			{
				throw new ArgumentNullException(nameof(content));
			}

			Options.DynamicTemplates[key] = content;
			return CompileRenderStringCoreAsync(key, content, model, modelType: null, viewBag, cancellationToken);
		}

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Task<string> CompileRenderStringAsync(
			string key,
			string content,
			object? model,
			Type modelType,
			ExpandoObject? viewBag = null) =>
			CompileRenderStringAsync(key, content, model, modelType, viewBag, CancellationToken.None);

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Task<string> CompileRenderStringAsync(
			string key,
			string content,
			object? model,
			Type modelType,
			ExpandoObject? viewBag,
			CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (string.IsNullOrEmpty(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			if (string.IsNullOrEmpty(content))
			{
				throw new ArgumentNullException(nameof(content));
			}

			if (modelType == null)
			{
				throw new ArgumentNullException(nameof(modelType));
			}

			Options.DynamicTemplates[key] = content;
			return CompileRenderStringCoreAsync(key, content, model, modelType, viewBag, cancellationToken);
		}

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		private async Task<string> CompileRenderStringCoreAsync(
			string key,
			string content,
			object? model,
			Type? modelType,
			ExpandoObject? viewBag,
			CancellationToken cancellationToken)
		{
			ITemplatePage template = await CompileTemplateAsync(TemplateCompilationRequest.ForString(
				key,
				content,
				modelType,
				Options.Namespaces), cancellationToken).ConfigureAwait(false);

			return modelType == null
				? await RenderTemplateAsync(template, model, viewBag, cancellationToken).ConfigureAwait(false)
				: await RenderTemplateAsync(template, model, modelType, viewBag, cancellationToken).ConfigureAwait(false);
		}

		private async Task<string> RenderTemplateAsync(
			ITemplatePage templatePage,
			object? model,
			Type modelType,
			ExpandoObject? viewBag,
			CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			using PageRenderLease renderLease = BeginRender(templatePage);
			using (var writer = new StringWriter())
			{
				Type effectiveModelType = GetDeclaredModelType(templatePage) ?? modelType;
				SetModelContext(templatePage, writer, model, viewBag, effectiveModelType, cancellationToken);

				using (var scope = new MemoryPoolViewBufferScope())
				{
					var renderer = new TemplateRenderer(this, scope);
					await renderer.RenderAsync(templatePage, cancellationToken).ConfigureAwait(false);
				}

				return writer.ToString();
			}
		}

		private void SetModelContext<T>(
			ITemplatePage templatePage,
			TextWriter textWriter,
			T model,
			ExpandoObject? viewBag,
			CancellationToken cancellationToken)
		{
			if (textWriter == null)
			{
				throw new ArgumentNullException(nameof(textWriter));
			}

			var pageContext = new PageContext(viewBag)
			{
				CancellationToken = cancellationToken,
				ExecutingPageKey = templatePage.Key,
				Writer = textWriter
			};

			if (model != null)
			{
				pageContext.ModelTypeInfo = new ModelTypeInfo(model.GetType());

				object? pageModel = pageContext.ModelTypeInfo.CreateTemplateModel(model);
				templatePage.SetModel(pageModel);

				pageContext.Model = pageModel;
			}
			else
			{
				templatePage.SetModel(null);
				pageContext.Model = null;
			}

			templatePage.PageContext = pageContext;
		}

		private void SetModelContext(
			ITemplatePage templatePage,
			TextWriter textWriter,
			object? model,
			ExpandoObject? viewBag,
			Type modelType,
			CancellationToken cancellationToken)
		{
			if (textWriter == null)
			{
				throw new ArgumentNullException(nameof(textWriter));
			}

			if (model != null && !modelType.IsInstanceOfType(model))
			{
				throw new ArgumentException(
					$"The supplied model of type '{model.GetType()}' is not assignable to '{modelType}'.",
					nameof(model));
			}

			var modelTypeInfo = new ModelTypeInfo(modelType);
			object? pageModel = model == null ? null : modelTypeInfo.CreateTemplateModel(model);
			templatePage.SetModel(pageModel);

			templatePage.PageContext = new PageContext(viewBag)
			{
				CancellationToken = cancellationToken,
				ExecutingPageKey = templatePage.Key,
				Writer = textWriter,
				ModelTypeInfo = modelTypeInfo,
				Model = pageModel
			};
		}

		private void InvalidatePreviousStringTemplate(TemplateCompilationRequest request)
		{
			if (!request.IsStringTemplate || Cache == null)
			{
				return;
			}

			if (Cache is ICoordinatedCachingProvider coordinatedCache)
			{
				coordinatedCache.PrepareStringTemplate(request.TemplateKey, request.CacheKey);
			}
			else
			{
				Cache.Remove(request.TemplateKey);
			}
		}

		private string NormalizeProjectKey(string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			return _compilerCache != null
				? _compilerCache.NormalizeKey(key)
				: key;
		}

		private void StoreCompiledTemplate(
			string templateKey,
			string cacheKey,
			Func<ITemplatePage> templateFactory,
			IChangeToken? expirationToken,
			long cacheVersion)
		{
			if (Cache is ICoordinatedCachingProvider coordinatedCache)
			{
				coordinatedCache.StoreCompiledTemplate(
					templateKey,
					cacheKey,
					templateFactory,
					expirationToken,
					cacheVersion);
			}
			else
			{
				Cache!.CacheTemplate(cacheKey, templateFactory, expirationToken);
			}
		}

		private static Type? GetDeclaredModelType(ITemplatePage templatePage)
		{
			for (Type? type = templatePage.GetType(); type != null; type = type.BaseType)
			{
				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(TemplatePage<>))
				{
					Type declaredType = type.GetGenericArguments()[0];
					return declaredType == typeof(object) ? null : declaredType;
				}
			}

			return null;
		}

		private PageRenderLease BeginRender(ITemplatePage page)
		{
			if (page == null)
			{
				throw new ArgumentNullException(nameof(page));
			}

			PageRenderState state = PageRenderStates.GetOrCreateValue(page);
			if (Interlocked.CompareExchange(ref state.Status, 1, 0) != 0)
			{
				throw new InvalidOperationException(
					"Template page instances are single-use and cannot be rendered concurrently or more than once. " +
					"Use CompileReusableTemplateAsync to render a compiled template repeatedly.");
			}

			return new PageRenderLease(state);
		}

		private sealed class PageRenderState
		{
			public int Status;
		}

		private readonly struct PageRenderLease : IDisposable
		{
			private readonly PageRenderState _state;

			public PageRenderLease(PageRenderState state)
			{
				_state = state;
			}

			public void Dispose() => Volatile.Write(ref _state.Status, 2);
		}
	}
}
