using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using RazorLight.Generation;
using RazorLight.Razor;

namespace RazorLight.Compilation
{
	internal class RazorTemplateCompiler : IRazorTemplateCompiler, IDisposable
	{
		private RazorSourceGenerator _razorSourceGenerator;
		private ICompilationService _compiler;

		private readonly RazorLightOptions _razorLightOptions;
		private readonly RazorLightProject _razorProject;
		private readonly IMemoryCache _cache;
		private readonly ConcurrentDictionary<string, string> _normalizedKeysCache;
		private readonly Dictionary<string, CompiledTemplateDescriptor> _precompiledViews;
		private readonly ConcurrentDictionary<string, string> _stringTemplateCacheKeys;
		private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _cacheKeysByTemplate;
		private readonly ConcurrentDictionary<string, object> _cacheGenerations;
		private readonly ConcurrentDictionary<CompilationIdentity, Lazy<Task<CompiledTemplateDescriptor>>> _compilations;

		public RazorTemplateCompiler(
			RazorSourceGenerator sourceGenerator,
			ICompilationService compilationService,
			RazorLightProject razorLightProject,
			RazorLightOptions razorLightOptions)
		{
			_razorSourceGenerator = sourceGenerator ?? throw new ArgumentNullException(nameof(sourceGenerator));
			_compiler = compilationService ?? throw new ArgumentNullException(nameof(compilationService));
			_razorProject = razorLightProject ?? throw new ArgumentNullException(nameof(razorLightProject));
			_razorLightOptions = razorLightOptions ?? throw new ArgumentNullException(nameof(razorLightOptions));

			// This is our L0 cache, and is a durable store. Views migrate into the cache as they are requested
			// from either the set of known precompiled views, or by being compiled.
			var cacheOptions = Options.Create(new MemoryCacheOptions());
			_cache = new MemoryCache(cacheOptions);

			_normalizedKeysCache = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
			_stringTemplateCacheKeys = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
			_cacheKeysByTemplate = new ConcurrentDictionary<string, ConcurrentDictionary<string, byte>>(StringComparer.Ordinal);
			_cacheGenerations = new ConcurrentDictionary<string, object>(StringComparer.Ordinal);
			_compilations = new ConcurrentDictionary<CompilationIdentity, Lazy<Task<CompiledTemplateDescriptor>>>();

			// We need to validate that the all of the precompiled views are unique by path (case-insensitive).
			// We do this because there's no good way to canonicalize paths on windows, and it will create
			// problems when deploying to linux. Rather than deal with these issues, we just don't support
			// views that differ only by case.
			_precompiledViews = new Dictionary<string, CompiledTemplateDescriptor>(
				5, //Change capacity when precompiled views are arrived
				StringComparer.OrdinalIgnoreCase);
		}

		public RazorTemplateCompiler(
			RazorSourceGenerator sourceGenerator,
			ICompilationService compilationService,
			RazorLightProject razorLightProject,
			IOptions<RazorLightOptions> options) : this(
				sourceGenerator,
				compilationService,
				razorLightProject,
				(options ?? throw new ArgumentNullException(nameof(options))).Value)
		{

		}

		internal IMemoryCache Cache => _cache;
		internal int ActiveCompilationCount => _compilations.Count;
		internal int CacheGenerationCount => _cacheGenerations.Count;
		internal bool IsDisposed { get; private set; }
		internal RazorLightProject Project => _razorProject;

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Task<CompiledTemplateDescriptor> CompileAsync(string templateKey)
			=> CompileAsync(templateKey, CancellationToken.None);

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Task<CompiledTemplateDescriptor> CompileAsync(string templateKey, CancellationToken cancellationToken)
		{
			return CompileAsync(TemplateCompilationRequest.ForProject(
				templateKey,
				modelType: null,
				_razorLightOptions.Namespaces), cancellationToken);
		}

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Task<CompiledTemplateDescriptor> CompileAsync(string templateKey, Type modelType)
			=> CompileAsync(templateKey, modelType, CancellationToken.None);

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Task<CompiledTemplateDescriptor> CompileAsync(string templateKey, Type modelType, CancellationToken cancellationToken)
		{
			if (modelType == null)
			{
				throw new ArgumentNullException(nameof(modelType));
			}

			return CompileAsync(TemplateCompilationRequest.ForProject(
				templateKey,
				modelType,
				_razorLightOptions.Namespaces), cancellationToken);
		}

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Task<CompiledTemplateDescriptor> CompileAsync(
			string templateKey,
			string templateContent,
			Type? modelType = null) =>
			CompileAsync(templateKey, templateContent, modelType, CancellationToken.None);

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Task<CompiledTemplateDescriptor> CompileAsync(
			string templateKey,
			string templateContent,
			Type? modelType,
			CancellationToken cancellationToken)
		{
			if (templateContent == null)
			{
				throw new ArgumentNullException(nameof(templateContent));
			}

			return CompileAsync(TemplateCompilationRequest.ForString(
				templateKey,
				templateContent,
				modelType,
				_razorLightOptions.Namespaces), cancellationToken);
		}

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		private async Task<CompiledTemplateDescriptor> CompileAsync(
			TemplateCompilationRequest request,
			CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Task<CompiledTemplateDescriptor> sharedCompilation = GetOrCreateCompilationAsync(request, cancellationToken);
			return await sharedCompilation.WaitAsync(cancellationToken).ConfigureAwait(false);
		}

		private Task<CompiledTemplateDescriptor> GetOrCreateCompilationAsync(
			TemplateCompilationRequest request,
			CancellationToken cancellationToken)
		{
			if (request.TemplateKey == null)
			{
				throw new ArgumentNullException("templateKey");
			}

			InvalidatePreviousStringTemplate(request);

			// Attempt to lookup the cache entry using the passed in key. This will succeed if the key is already
			// normalized and a cache entry exists.
			if (_cache.TryGetValue(request.CacheKey, out object? cachedValue) && cachedValue is Task<CompiledTemplateDescriptor> cachedResult)
			{
				return cachedResult;
			}

			string normalizedPath = request.ModelType == null && !request.IsStringTemplate
				? GetNormalizedKey(request.TemplateKey)
				: request.CacheKey;
			if (_cache.TryGetValue(normalizedPath, out cachedValue) && cachedValue is Task<CompiledTemplateDescriptor> normalizedResult)
			{
				return normalizedResult;
			}

			// Entry does not exist. Attempt to create one.
			return OnCacheMissAsync(request, cancellationToken);
		}

		/// <summary>
		/// For testing purposes only.
		/// </summary>
		internal Type ProjectType => _razorProject.GetType();

		private Task<CompiledTemplateDescriptor> OnCacheMissAsync(
			TemplateCompilationRequest request,
			CancellationToken cancellationToken)
		{
			string normalizedKey = request.ModelType == null && !request.IsStringTemplate
				? GetNormalizedKey(request.TemplateKey)
				: request.CacheKey;
			string templateKey = request.IsStringTemplate
				? request.TemplateKey
				: GetNormalizedKey(request.TemplateKey);
			cancellationToken.ThrowIfCancellationRequested();
			object generation = _cacheGenerations.GetOrAdd(templateKey, _ => new object());
			var identity = new CompilationIdentity(normalizedKey, templateKey, generation);
			var candidate = new Lazy<Task<CompiledTemplateDescriptor>>(
				() => CompileAndCacheAsync(request, normalizedKey, templateKey, generation),
				LazyThreadSafetyMode.ExecutionAndPublication);
			Lazy<Task<CompiledTemplateDescriptor>> compilation = _compilations.GetOrAdd(identity, candidate);
			return AwaitCompilationAsync(identity, compilation);
		}

		private async Task<CompiledTemplateDescriptor> AwaitCompilationAsync(
			CompilationIdentity identity,
			Lazy<Task<CompiledTemplateDescriptor>> compilation)
		{
			try
			{
				return await compilation.Value.ConfigureAwait(false);
			}
			finally
			{
				_compilations.TryRemove(
					new KeyValuePair<CompilationIdentity, Lazy<Task<CompiledTemplateDescriptor>>>(identity, compilation));
				if (compilation.IsValueCreated && !compilation.Value.IsCompletedSuccessfully)
				{
					_stringTemplateCacheKeys.TryRemove(
						new KeyValuePair<string, string>(identity.TemplateKey, identity.CacheKey));
				}
				TryRemoveUnusedGeneration(identity.TemplateKey, identity.Generation);
			}
		}

		private async Task<CompiledTemplateDescriptor> CompileAndCacheAsync(
			TemplateCompilationRequest request,
			string normalizedKey,
			string templateKey,
			object generation)
		{
			ViewCompilerWorkItem item;
			if (request.ModelType == null && !request.IsStringTemplate &&
				_precompiledViews.TryGetValue(normalizedKey, out var precompiledView))
			{
				item = new ViewCompilerWorkItem
				{
					NormalizedKey = normalizedKey,
					ExpirationToken = precompiledView.ExpirationToken,
					Descriptor = precompiledView,
				};
			}
			else
			{
				item = await CreateRuntimeCompilationWorkItem(request, CancellationToken.None).ConfigureAwait(false);
			}

			CompiledTemplateDescriptor descriptor;
			if (item.SupportsCompilation)
			{
				var projectItem = item.ProjectItem
					?? throw new InvalidOperationException("The compilation work item has no project item.");
				var generatedTemplate = item.GeneratedTemplate
					?? throw new InvalidOperationException("The compilation work item has no generated template.");
				descriptor = CompileAndEmit(projectItem, generatedTemplate);
				descriptor.ExpirationToken = item.ExpirationToken;
			}
			else
			{
				descriptor = item.Descriptor
					?? throw new InvalidOperationException("The precompiled work item has no descriptor.");
			}

			var cacheEntryOptions = new MemoryCacheEntryOptions();
			if (item.ExpirationToken != null)
			{
				cacheEntryOptions.ExpirationTokens.Add(item.ExpirationToken);
			}

			if (_cacheGenerations.TryGetValue(templateKey, out object? currentGeneration) &&
				ReferenceEquals(currentGeneration, generation))
			{
				Task<CompiledTemplateDescriptor> completed = Task.FromResult(descriptor);
				RegisterCacheKey(templateKey, item.NormalizedKey, generation, cacheEntryOptions);
				_ = _cache.Set(item.NormalizedKey, completed, cacheEntryOptions);
				if (request.IsStringTemplate)
				{
					RegisterCacheKey(templateKey, request.TemplateKey, generation, cacheEntryOptions);
					_ = _cache.Set(request.TemplateKey, completed, cacheEntryOptions);
				}
			}

			return descriptor;
		}

		private async Task<ViewCompilerWorkItem> CreateRuntimeCompilationWorkItem(
			TemplateCompilationRequest request,
			CancellationToken cancellationToken)
		{
			RazorLightProjectItem projectItem;

			if (request.TemplateContent != null)
			{
				projectItem = new TextSourceRazorProjectItem(request.TemplateKey, request.TemplateContent);
			}
			else if (_razorLightOptions.DynamicTemplates.TryGetValue(request.TemplateKey, out string? templateContent) && templateContent != null)
			{
				projectItem = new TextSourceRazorProjectItem(request.TemplateKey, templateContent);
			}
			else
			{
				string normalizedKey = GetNormalizedKey(request.TemplateKey);
				projectItem = await _razorProject.GetItemAsync(normalizedKey, cancellationToken).ConfigureAwait(false);
			}

			if (!projectItem.Exists)
			{
				var templateNotFoundException = await CreateTemplateNotFoundException(projectItem, cancellationToken).ConfigureAwait(false);
				throw templateNotFoundException;
			}

			IGeneratedRazorTemplate generatedTemplate = request.ModelType == null
				? await _razorSourceGenerator.GenerateCodeAsync(projectItem, cancellationToken).ConfigureAwait(false)
				: await _razorSourceGenerator.GenerateCodeAsync(projectItem, request.ModelType, cancellationToken).ConfigureAwait(false);
			var expirationTokens = new List<IChangeToken>();
			if (projectItem.ExpirationToken != null) expirationTokens.Add(projectItem.ExpirationToken);
			if (generatedTemplate is IGeneratedCSharpSourceContainer sourceContainer)
			{
				foreach (CSharpSourceDocument source in sourceContainer.CSharpSources)
				{
					if (source.ExpirationToken != null) expirationTokens.Add(source.ExpirationToken);
				}
			}

			return new ViewCompilerWorkItem
			{
				SupportsCompilation = true,

				ProjectItem = projectItem,
				NormalizedKey = request.ModelType == null && !request.IsStringTemplate
					? projectItem.Key
					: request.CacheKey,
				ExpirationToken = expirationTokens.Count switch
				{
					0 => null,
					1 => expirationTokens[0],
					_ => new CompositeChangeToken(expirationTokens),
				},
				GeneratedTemplate = generatedTemplate,
			};
		}

		[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "This method is reachable only from the runtime-compilation API, which is annotated as trim-unsafe.")]
		private CompiledTemplateDescriptor CompileAndEmit(
			RazorLightProjectItem projectItem,
			IGeneratedRazorTemplate generatedTemplate)
		{
			Assembly assembly = _compiler.CompileAndEmit(generatedTemplate);

			// Anything we compile from source will use Razor 2.1 and so should have the new metadata.
			var attribute = assembly.GetCustomAttribute<RazorLightTemplateAttribute>();

			return new CompiledTemplateDescriptor
			{
				TemplateKey = projectItem.Key,
				TemplateAttribute = attribute
			};
		}

		private void InvalidatePreviousStringTemplate(TemplateCompilationRequest request)
		{
			if (!request.IsStringTemplate)
			{
				return;
			}

			_stringTemplateCacheKeys.AddOrUpdate(
				request.TemplateKey,
				templateKey =>
				{
					_cacheGenerations.TryRemove(request.TemplateKey, out _);
					_cache.Remove(request.TemplateKey);
					return request.CacheKey;
				},
				(templateKey, previousCacheKey) =>
				{
					if (!string.Equals(previousCacheKey, request.CacheKey, StringComparison.Ordinal))
					{
						_cacheGenerations.TryRemove(request.TemplateKey, out _);
						_cache.Remove(request.TemplateKey);
						_cache.Remove(previousCacheKey);
					}

					return request.CacheKey;
				});
		}

		private void RegisterCacheKey(
			string templateKey,
			string cacheKey,
			object generation,
			MemoryCacheEntryOptions cacheEntryOptions)
		{
			var keys = _cacheKeysByTemplate.GetOrAdd(
				templateKey,
				_ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal));
			keys.TryAdd(cacheKey, 0);
			cacheEntryOptions.RegisterPostEvictionCallback((_, _, _, _) =>
			{
				keys.TryRemove(cacheKey, out _);
				if (keys.IsEmpty)
				{
					_cacheKeysByTemplate.TryRemove(
						new KeyValuePair<string, ConcurrentDictionary<string, byte>>(templateKey, keys));
					_stringTemplateCacheKeys.TryRemove(
						new KeyValuePair<string, string>(templateKey, cacheKey));
					TryRemoveUnusedGeneration(templateKey, generation);
				}
			});
		}

		internal string NormalizeCacheKey(string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			return GetNormalizedKey(key);
		}

		internal void RemoveCacheKey(string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			string normalizedKey = GetNormalizedKey(key);
			_cacheGenerations.TryRemove(key, out _);
			RemoveCacheKeys(key);
			if (!string.Equals(key, normalizedKey, StringComparison.Ordinal))
			{
				_cacheGenerations.TryRemove(normalizedKey, out _);
				RemoveCacheKeys(normalizedKey);
			}
		}

		private void TryRemoveUnusedGeneration(string templateKey, object generation)
		{
			if (_cacheKeysByTemplate.ContainsKey(templateKey) ||
				_compilations.Keys.Any(identity =>
					string.Equals(identity.TemplateKey, templateKey, StringComparison.Ordinal) &&
					ReferenceEquals(identity.Generation, generation)))
			{
				return;
			}

			_cacheGenerations.TryRemove(new KeyValuePair<string, object>(templateKey, generation));
		}

		private void RemoveCacheKeys(string templateKey)
		{
			_cache.Remove(templateKey);
			if (_cacheKeysByTemplate.TryRemove(templateKey, out ConcurrentDictionary<string, byte>? keys))
			{
				foreach (string cacheKey in keys.Keys)
				{
					_cache.Remove(cacheKey);
				}
			}

			if (_stringTemplateCacheKeys.TryRemove(templateKey, out string? stringCacheKey))
			{
				_cache.Remove(stringCacheKey);
			}
		}

		#region helpers

		internal string GetNormalizedKey(string templateKey)
		{
			Debug.Assert(templateKey != null);

			//Support path normalization only on Filesystem projects
			if (!(_razorProject is FileSystemRazorProject))
			{
				return templateKey;
			}

			if (templateKey.Length == 0)
			{
				return templateKey;
			}

			if (_normalizedKeysCache.TryGetValue(templateKey, out var normalizedPath))
				return normalizedPath;

			normalizedPath = _razorProject.NormalizeKey(templateKey);
			_normalizedKeysCache[templateKey] = normalizedPath;

			return normalizedPath;
		}

		internal Task<TemplateNotFoundException> CreateTemplateNotFoundException(RazorLightProjectItem projectItem) =>
			CreateTemplateNotFoundException(projectItem, CancellationToken.None);

		public void Dispose()
		{
			if (IsDisposed) return;
			_cache.Dispose();
			IsDisposed = true;
		}

		internal async Task<TemplateNotFoundException> CreateTemplateNotFoundException(
			RazorLightProjectItem projectItem,
			CancellationToken cancellationToken)
		{
			var propNames = $"\"{nameof(TemplateNotFoundException.KnownDynamicTemplateKeys)}\" and \"{nameof(TemplateNotFoundException.KnownProjectTemplateKeys)}\"";

			if (_razorLightOptions.EnableDebugMode ?? false)
			{
				var msg = $"{nameof(RazorLightProjectItem)} of type {projectItem.GetType().FullName} with key {projectItem.Key} could not be found by the " +
					$"{nameof(RazorLightProject)} of type {_razorProject.GetType().FullName} and does not exist in dynamic templates. ";
				msg += $"See the {propNames} properties for known template keys.";

				var dynamicKeys = _razorLightOptions.DynamicTemplates.Keys.ToList();

				var projectKeys = await _razorProject.GetKnownKeysAsync(cancellationToken).ConfigureAwait(false);
				projectKeys = projectKeys?.ToList() ?? Enumerable.Empty<string>();

				return new TemplateNotFoundException(msg, dynamicKeys, projectKeys);
			}
			else
			{
				var msg = "The requested template could not be found. " +
					$"Set {nameof(RazorLightOptions)}.{nameof(RazorLightOptions.EnableDebugMode)} to true to allow " +
					$"the {propNames} properties on this exception to be set.";

				return new TemplateNotFoundException(msg);
			}
		}

		private class ViewCompilerWorkItem
		{
			public bool SupportsCompilation { get; set; }

			public string NormalizedKey { get; set; } = string.Empty;

			public IChangeToken? ExpirationToken { get; set; }

			// ReSharper disable once UnusedAutoPropertyAccessor.Local
			public CompiledTemplateDescriptor? Descriptor { get; set; }

			public RazorLightProjectItem? ProjectItem { get; set; }

			public IGeneratedRazorTemplate? GeneratedTemplate { get; set; }
		}

		private readonly record struct CompilationIdentity(
			string CacheKey,
			string TemplateKey,
			object Generation);

		#endregion
	}
}
