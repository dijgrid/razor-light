using Microsoft.Extensions.Primitives;
using RazorLight.Compilation;
using RazorLight.Generation;
using RazorLight.Razor;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;

namespace RazorLight.Caching
{
	public sealed class FileSystemCachingProvider : ICachingProvider, IPrecompileCallback
	{
		private readonly MemoryCachingProvider m_cache = new MemoryCachingProvider();
		private readonly string m_baseDir;
		private readonly string m_cacheDir;
		private readonly bool m_reportRelativeCachePaths;
		private readonly IFileSystemCachingStrategy m_fileSystemCachingStrategy;

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
			if (rawSymbolStore != null)
			{
				File.WriteAllBytes(pdbFilePath, rawSymbolStore);
			}
		}

		public void CacheTemplate(string key, Func<ITemplatePage> pageFactory, IChangeToken? expirationToken)
		{
			m_cache.CacheTemplate(key, pageFactory, expirationToken);
		}

		public bool Contains(string key) => m_cache.Contains(key) ||
			m_fileSystemCachingStrategy.GetCachedFileInfo(key, GetSourceFilePath(key), m_cacheDir).UpToDate;

		public void Remove(string key)
		{
			m_cache.Remove(key);
			var srcFilePath = GetSourceFilePath(key);
			var (_, asmFilePath, pdbFilePath) = m_fileSystemCachingStrategy.GetCachedFileInfo(key, srcFilePath, m_cacheDir);
			File.Delete(asmFilePath);
			File.Delete(pdbFilePath);
		}

		public bool TryGetTemplate(string key, [NotNullWhen(true)] out Func<ITemplatePage>? pageFactory)
		{
			if (m_cache.TryGetTemplate(key, out pageFactory))
			{
				return true;
			}

			var srcFilePath = GetSourceFilePath(key);
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

		public static Type GetTemplatePageType(string asmFilePath)
		{
			var rawAssembly = File.ReadAllBytes(asmFilePath);
			var pdbFilePath = asmFilePath.Replace(".dll", ".pdb");
			var rawSymbolStore = File.Exists(pdbFilePath) ? File.ReadAllBytes(pdbFilePath) : null;
			return GetTemplatePageType(rawAssembly, rawSymbolStore);
		}

		public static ITemplatePage NewTemplatePage(Type templatePageType) =>
			(ITemplatePage)(Activator.CreateInstance(templatePageType)
				?? throw new InvalidOperationException($"Could not create template page type '{templatePageType}'."));

		public static Type GetTemplatePageType(byte[] rawAssembly, byte[]? rawSymbolStore) => Assembly
			.Load(rawAssembly, rawSymbolStore)
			.GetCustomAttribute<RazorLightTemplateAttribute>()
			?.TemplateType
			?? throw new InvalidOperationException("The cached assembly has no RazorLight template attribute.");

		private string GetSourceFilePath(string key) =>
			FileSystemRazorProjectHelper.ResolveContainedPath(m_baseDir, key, "template path");
	}
}
