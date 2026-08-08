using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using RazorLight.Caching;
using RazorLight.Compilation;
using RazorLight.Internal.Buffering;

namespace RazorLight
{
	public class EngineHandler : IEngineHandler
	{
		private readonly ConcurrentDictionary<string, string> _stringTemplateCacheKeys =
			new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
		private readonly ITemplateCompilerCache? _compilerCache;

		public EngineHandler(
			RazorLightOptions options,
			IRazorTemplateCompiler compiler,
			ITemplateFactoryProvider factoryProvider,
			ICachingProvider? cache)
		{
			Options = options ?? throw new ArgumentNullException(nameof(options));
			Compiler = compiler ?? throw new ArgumentNullException(nameof(compiler));
			FactoryProvider = factoryProvider ?? throw new ArgumentNullException(nameof(factoryProvider));

			_compilerCache = compiler is RazorTemplateCompiler razorTemplateCompiler
				? new RazorTemplateCompilerCache(razorTemplateCompiler)
				: null;
			Cache = cache != null && _compilerCache != null
				? new CoordinatedCachingProvider(cache, _compilerCache)
				: cache;
			Options.CachingProvider = Cache;
		}

		public EngineHandler(
			IOptions<RazorLightOptions> options,
			IRazorTemplateCompiler compiler,
			ITemplateFactoryProvider factoryProvider,
			ICachingProvider? cache) : this(
				(options ?? throw new ArgumentNullException(nameof(options))).Value,
				compiler,
				factoryProvider,
				cache)
		{


		}

		public RazorLightOptions Options { get; }
		public ICachingProvider? Cache { get; }
		public IRazorTemplateCompiler Compiler { get; }
		public ITemplateFactoryProvider FactoryProvider { get; }

		[MemberNotNullWhen(true, nameof(Cache))]
		public bool IsCachingEnabled => Cache != null;

		/// <summary>
		/// Search and compile a template with a given key
		/// </summary>
		/// <param name="key">Unique key of the template</param>
		/// <returns>An instance of a template</returns>
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public async Task<ITemplatePage> CompileTemplateAsync(string key)
		{
			return await CompileTemplateAsync(TemplateCompilationRequest.ForProject(
				NormalizeProjectKey(key),
				modelType: null,
				Options.Namespaces));
		}

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		private async Task<ITemplatePage> CompileTemplateAsync(TemplateCompilationRequest request)
		{
			InvalidatePreviousStringTemplate(request);
			var coordinatedCache = Cache as ICoordinatedCachingProvider;
			long cacheVersion = coordinatedCache?.GetVersion(request.TemplateKey) ?? 0;

			ITemplatePage? templatePage = null;
			if (Cache != null)
			{
				var cacheLookupResult = Cache.RetrieveTemplate(request.CacheKey);
				if (cacheLookupResult.Success)
				{
					templatePage = cacheLookupResult.Template.TemplatePageFactory();
				}
			}

			if (templatePage == null)
			{
				CompiledTemplateDescriptor templateDescriptor;
				if (request.TemplateContent != null)
				{
					templateDescriptor = await Compiler.CompileAsync(
						request.TemplateKey,
						request.TemplateContent,
						request.ModelType);
				}
				else if (request.ModelType != null)
				{
					templateDescriptor = await Compiler.CompileAsync(request.TemplateKey, request.ModelType);
				}
				else
				{
					templateDescriptor = await Compiler.CompileAsync(request.TemplateKey);
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

		/// <summary>
		/// Renders a template with a given model
		/// </summary>
		/// <param name="templatePage">Instance of a template</param>
		/// <param name="model">Template model</param>
		/// <param name="viewBag">Dynamic viewBag of the template</param>
		/// <returns>Rendered string</returns>
		public async Task<string> RenderTemplateAsync<T>(ITemplatePage templatePage, T model, ExpandoObject? viewBag = null)
		{
			using (var writer = new StringWriter())
			{
				await RenderTemplateAsync(templatePage, model, writer, viewBag);

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
			ExpandoObject? viewBag = null)
		{
			SetModelContext(templatePage, textWriter, model, viewBag);

			using (var scope = new MemoryPoolViewBufferScope())
			{
				var renderer = new TemplateRenderer(this, scope);
				await renderer.RenderAsync(templatePage).ConfigureAwait(false);
			}
		}

		public async Task RenderIncludedTemplateAsync<T>(
			ITemplatePage templatePage,
			T model,
			TextWriter textWriter,
			ExpandoObject? viewBag,
			TemplateRenderer templateRenderer)
		{
			SetModelContext(templatePage, textWriter, model, viewBag);
			await templateRenderer.RenderAsync(templatePage).ConfigureAwait(false);
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
		{
			ITemplatePage template = await CompileTemplateAsync(key).ConfigureAwait(false);

			return await RenderTemplateAsync(template, model, viewBag).ConfigureAwait(false);
		}

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public async Task<string> CompileRenderAsync(
			string key,
			object? model,
			Type modelType,
			ExpandoObject? viewBag = null)
		{
			if (modelType == null)
			{
				throw new ArgumentNullException(nameof(modelType));
			}

			ITemplatePage template = await CompileTemplateAsync(TemplateCompilationRequest.ForProject(
				NormalizeProjectKey(key),
				modelType,
				Options.Namespaces)).ConfigureAwait(false);

			return await RenderTemplateAsync(template, model, modelType, viewBag).ConfigureAwait(false);
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
			ExpandoObject? viewBag = null)
		{
			if (string.IsNullOrEmpty(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			if (string.IsNullOrEmpty(content))
			{
				throw new ArgumentNullException(nameof(content));
			}

			Options.DynamicTemplates[key] = content;
			return CompileRenderStringCoreAsync(key, content, model, modelType: null, viewBag);
		}

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Task<string> CompileRenderStringAsync(
			string key,
			string content,
			object? model,
			Type modelType,
			ExpandoObject? viewBag = null)
		{
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
			return CompileRenderStringCoreAsync(key, content, model, modelType, viewBag);
		}

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		private async Task<string> CompileRenderStringCoreAsync(
			string key,
			string content,
			object? model,
			Type? modelType,
			ExpandoObject? viewBag)
		{
			ITemplatePage template = await CompileTemplateAsync(TemplateCompilationRequest.ForString(
				key,
				content,
				modelType,
				Options.Namespaces)).ConfigureAwait(false);

			return modelType == null
				? await RenderTemplateAsync(template, model, viewBag).ConfigureAwait(false)
				: await RenderTemplateAsync(template, model, modelType, viewBag).ConfigureAwait(false);
		}

		private async Task<string> RenderTemplateAsync(
			ITemplatePage templatePage,
			object? model,
			Type modelType,
			ExpandoObject? viewBag)
		{
			using (var writer = new StringWriter())
			{
				Type effectiveModelType = GetDeclaredModelType(templatePage) ?? modelType;
				SetModelContext(templatePage, writer, model, viewBag, effectiveModelType);

				using (var scope = new MemoryPoolViewBufferScope())
				{
					var renderer = new TemplateRenderer(this, scope);
					await renderer.RenderAsync(templatePage).ConfigureAwait(false);
				}

				return writer.ToString();
			}
		}

		private void SetModelContext<T>(
			ITemplatePage templatePage,
			TextWriter textWriter,
			T model,
			ExpandoObject? viewBag)
		{
			if (textWriter == null)
			{
				throw new ArgumentNullException(nameof(textWriter));
			}

			var pageContext = new PageContext(viewBag)
			{
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

			templatePage.PageContext = pageContext;
		}

		private void SetModelContext(
			ITemplatePage templatePage,
			TextWriter textWriter,
			object? model,
			ExpandoObject? viewBag,
			Type modelType)
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

			_stringTemplateCacheKeys.AddOrUpdate(
				request.TemplateKey,
				_ =>
				{
					Cache.Remove(request.TemplateKey);
					return request.CacheKey;
				},
				(_, previousCacheKey) =>
				{
					if (!string.Equals(previousCacheKey, request.CacheKey, StringComparison.Ordinal))
					{
						Cache.Remove(request.TemplateKey);
						Cache.Remove(previousCacheKey);
					}

					return request.CacheKey;
				});
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
	}
}
