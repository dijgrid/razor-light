using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Primitives;
using RazorLight.Compilation;
using RazorLight.Generation;
using RazorLight.Razor;

namespace RazorLight.Caching
{
	/// <summary>Persists precompiled template assemblies to disk and caches loaded page factories.</summary>
	public sealed class FileSystemCachingProvider : ICachingProvider, IPrecompileCallback, IDisposable
	{
		private const string CacheKeyManifestSuffix = ".razorlight-cache-key";
		private readonly MemoryCachingProvider m_cache = new MemoryCachingProvider();
		private readonly string m_baseDir;
		private readonly string m_cacheDir;
		private readonly bool m_reportRelativeCachePaths;
		private readonly IFileSystemCachingStrategy m_fileSystemCachingStrategy;

		/// <summary>Creates a provider rooted at a template directory and contained cache directory.</summary>
		public FileSystemCachingProvider(string baseDir, string cacheDir, IFileSystemCachingStrategy fileSystemCachingStrategy)
		{
			m_baseDir = FileSystemRazorProjectHelper.NormalizeRoot(
				baseDir ?? throw new ArgumentNullException(nameof(baseDir)));
			m_reportRelativeCachePaths = !Path.IsPathFullyQualified(
				cacheDir ?? throw new ArgumentNullException(nameof(cacheDir)));
			m_cacheDir = FileSystemRazorProjectHelper.NormalizeRoot(
				cacheDir);
			m_fileSystemCachingStrategy = fileSystemCachingStrategy ?? throw new ArgumentNullException(nameof(fileSystemCachingStrategy));
		}

		/// <summary>Returns the assembly path selected for a logical template and source file.</summary>
		public string GetAssemblyFilePath(string key, string templateFilePath)
		{
			var assemblyFilePath = m_fileSystemCachingStrategy
				.GetCachedFileInfo(key, templateFilePath, m_cacheDir)
				.AssemblyFilePath;

			return m_reportRelativeCachePaths
				? Path.GetRelativePath(Environment.CurrentDirectory, assemblyFilePath)
				: assemblyFilePath;
		}

		void IPrecompileCallback.Invoke(IGeneratedRazorTemplate generatedRazorTemplate, byte[] rawAssembly, byte[] rawSymbolStore)
		{
			var srcFilePath = GetSourceFilePath(generatedRazorTemplate.TemplateKey);
			var (_, asmFilePath, pdbFilePath) = m_fileSystemCachingStrategy.GetCachedFileInfo(generatedRazorTemplate.TemplateKey, srcFilePath, m_cacheDir);
			Directory.CreateDirectory(Path.GetDirectoryName(asmFilePath)
				?? throw new InvalidOperationException($"The cache path '{asmFilePath}' has no directory."));
			File.WriteAllBytes(asmFilePath, rawAssembly);
			File.WriteAllText(asmFilePath + CacheKeyManifestSuffix, generatedRazorTemplate.TemplateKey);
			if (rawSymbolStore != null)
			{
				File.WriteAllBytes(pdbFilePath, rawSymbolStore);
			}
		}

		/// <inheritdoc />
		public void CacheTemplate(string key, Func<ITemplatePage> pageFactory, IChangeToken? expirationToken)
		{
			m_cache.CacheTemplate(key, pageFactory, expirationToken);
		}

		/// <inheritdoc />
		public bool Contains(string key)
		{
			if (m_cache.Contains(key)) return true;
			string sourcePath = GetSourceFilePath(key);
			return File.Exists(sourcePath) &&
				m_fileSystemCachingStrategy.GetCachedFileInfo(key, sourcePath, m_cacheDir).UpToDate;
		}

		/// <inheritdoc />
		public void Remove(string key)
		{
			m_cache.Remove(key);
			var srcFilePath = GetSourceFilePath(key);
			var (_, asmFilePath, pdbFilePath) = m_fileSystemCachingStrategy.GetCachedFileInfo(key, srcFilePath, m_cacheDir);
			File.Delete(asmFilePath);
			File.Delete(pdbFilePath);
			File.Delete(asmFilePath + CacheKeyManifestSuffix);
			RemoveManifestedArtifacts(key);
		}

		/// <inheritdoc />
		public bool TryGetTemplate(string key, [NotNullWhen(true)] out Func<ITemplatePage>? pageFactory)
		{
			if (m_cache.TryGetTemplate(key, out pageFactory))
			{
				return true;
			}

			var srcFilePath = GetSourceFilePath(key);
			if (!File.Exists(srcFilePath))
			{
				pageFactory = null;
				return false;
			}

			var (upToDate, asmFilePath, pdbFilePath) = m_fileSystemCachingStrategy.GetCachedFileInfo(key, srcFilePath, m_cacheDir);
			if (upToDate)
			{
				var rawAssembly = File.ReadAllBytes(asmFilePath);
				var rawSymbolStore = File.Exists(pdbFilePath) ? File.ReadAllBytes(pdbFilePath) : null;
				pageFactory = CreateTemplatePage;
				return true;

				ITemplatePage CreateTemplatePage()
				{
					var templatePageType = GetTemplatePageType(rawAssembly, rawSymbolStore);
					m_cache.CacheTemplate(key, CreateTemplatePage2);
					return CreateTemplatePage2();

					ITemplatePage CreateTemplatePage2() => NewTemplatePage(templatePageType);
				}
			}
			pageFactory = null;
			return false;
		}

		/// <inheritdoc />
		public static Type GetTemplatePageType(string asmFilePath)
		{
			var rawAssembly = File.ReadAllBytes(asmFilePath);
			var pdbFilePath = asmFilePath.Replace(".dll", ".pdb");
			var rawSymbolStore = File.Exists(pdbFilePath) ? File.ReadAllBytes(pdbFilePath) : null;
			return GetTemplatePageType(rawAssembly, rawSymbolStore);
		}

		/// <summary>Creates a generated template page using its public parameterless constructor.</summary>
		[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Runtime-loaded template types carry a public parameterless constructor by generated-code contract.")]
		public static ITemplatePage NewTemplatePage(Type templatePageType) =>
			(ITemplatePage)(Activator.CreateInstance(templatePageType)
				?? throw new InvalidOperationException($"Could not create template page type '{templatePageType}'."));

		/// <summary>Loads a generated template type from assembly and optional symbol bytes.</summary>
		[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "This API is the explicitly dynamic assembly-loading path and is not used by statically registered precompiled pages.")]
		public static Type GetTemplatePageType(byte[] rawAssembly, byte[]? rawSymbolStore) => Assembly
			.Load(rawAssembly, rawSymbolStore)
			.GetCustomAttribute<RazorLightTemplateAttribute>()
			?.TemplateType
			?? throw new InvalidOperationException("The cached assembly has no RazorLight template attribute.");

		private string GetSourceFilePath(string key) =>
			FileSystemRazorProjectHelper.ResolveContainedPath(m_baseDir, key, "template path");

		private void RemoveManifestedArtifacts(string key)
		{
			if (!Directory.Exists(m_cacheDir)) return;

			foreach (string manifestPath in Directory.EnumerateFiles(
				m_cacheDir,
				"*" + CacheKeyManifestSuffix,
				SearchOption.AllDirectories))
			{
				if (!string.Equals(File.ReadAllText(manifestPath), key, StringComparison.Ordinal))
				{
					continue;
				}

				string assemblyPath = manifestPath.Substring(
					0,
					manifestPath.Length - CacheKeyManifestSuffix.Length);
				File.Delete(assemblyPath);
				File.Delete(Path.ChangeExtension(assemblyPath, ".pdb"));
				File.Delete(manifestPath);
			}
		}

		/// <inheritdoc />
		public void Dispose() => m_cache.Dispose();
	}
}
