using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using RazorLight.Razor;

namespace RazorLight.Caching
{
	/// <summary>Selects disk-cache artifacts using a streamed SHA-256 fingerprint of compilation inputs.</summary>
	public sealed class FileHashCachingStrategy : IFileSystemCachingStrategy
	{
		private const string CacheFormatVersion = "razorlight-file-cache-v2";
		private static readonly string[] DependencyExtensions = { ".cshtml", ".razor", ".cs" };

		/// <summary>Gets the shared stateless strategy instance.</summary>
		public static readonly IFileSystemCachingStrategy Instance = new FileHashCachingStrategy();

		/// <inheritdoc />
		public string Name => "FileHash";

		/// <inheritdoc />
		public CachedFileInfo GetCachedFileInfo(string key, string templateFilePath, string cacheDir)
		{
			if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
			if (string.IsNullOrEmpty(templateFilePath)) throw new ArgumentNullException(nameof(templateFilePath));
			if (string.IsNullOrEmpty(cacheDir)) throw new ArgumentNullException(nameof(cacheDir));

			string projectRoot = DetermineProjectRoot(key, templateFilePath);
			string fingerprint = CreateFingerprint(key, templateFilePath, projectRoot);
			string asmFilePath = Path.Combine(cacheDir, fingerprint + ".dll");
			string pdbFilePath = Path.Combine(cacheDir, fingerprint + ".pdb");
			return new CachedFileInfo(File.Exists(asmFilePath), asmFilePath, pdbFilePath);
		}

		[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The build-time file cache fingerprints the compiler assembly reference identity.")]
		internal static string CreateFingerprint(string key, string templateFilePath, string projectRoot)
		{
			using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
			AppendText(hash, CacheFormatVersion);
			AppendText(hash, FileSystemRazorProjectHelper.NormalizeKey(key));
			AppendText(hash, typeof(FileHashCachingStrategy).Assembly.GetName().Version?.ToString() ?? "0.0.0.0");
			AppendText(hash, AppContext.TargetFrameworkName ?? "unknown-target-framework");
			AppendText(hash, "model-contract:dynamic");
			var defaultOptions = new RazorLightOptions();
			AppendText(hash, $"debug:{defaultOptions.EnableDebugMode ?? false};encoder:plain-text");
			AppendText(hash, "reference-discovery:" + defaultOptions.MetadataReferenceDiscovery);
			foreach (string @namespace in defaultOptions.Namespaces.OrderBy(value => value, StringComparer.Ordinal))
			{
				AppendText(hash, "namespace:" + @namespace);
			}

			foreach (AssemblyName reference in typeof(FileHashCachingStrategy).Assembly
				.GetReferencedAssemblies()
				.OrderBy(reference => reference.FullName, StringComparer.Ordinal))
			{
				AppendText(hash, reference.FullName ?? reference.Name ?? string.Empty);
			}

			if (Directory.Exists(projectRoot))
			{
				foreach (string dependencyPath in Directory
					.EnumerateFiles(projectRoot, "*", SearchOption.AllDirectories)
					.Where(IsTemplateDependency)
					.OrderBy(path => Path.GetRelativePath(projectRoot, path).Replace('\\', '/'), StringComparer.Ordinal))
				{
					AppendText(hash, Path.GetRelativePath(projectRoot, dependencyPath).Replace('\\', '/'));
					AppendFile(hash, dependencyPath);
				}
			}
			else if (File.Exists(templateFilePath))
			{
				AppendText(hash, Path.GetFileName(templateFilePath));
				AppendFile(hash, templateFilePath);
			}
			else
			{
				AppendText(hash, "missing-source");
			}

			return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
		}

		private static string DetermineProjectRoot(string key, string templateFilePath)
		{
			string fullTemplatePath = Path.GetFullPath(templateFilePath);
			string relativeKey = key.TrimStart('/', '\\')
				.Replace('/', Path.DirectorySeparatorChar)
				.Replace('\\', Path.DirectorySeparatorChar);
			StringComparison comparison = OperatingSystem.IsWindows()
				? StringComparison.OrdinalIgnoreCase
				: StringComparison.Ordinal;

			if (fullTemplatePath.EndsWith(relativeKey, comparison))
			{
				string root = fullTemplatePath.Substring(0, fullTemplatePath.Length - relativeKey.Length);
				return Path.TrimEndingDirectorySeparator(root);
			}

			return Path.GetDirectoryName(fullTemplatePath)
				?? throw new InvalidOperationException($"Could not determine the project root for '{templateFilePath}'.");
		}

		private static bool IsTemplateDependency(string path) =>
			DependencyExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

		private static void AppendText(IncrementalHash hash, string value)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(value);
			Span<byte> length = stackalloc byte[sizeof(int)];
			BinaryPrimitives.WriteInt32LittleEndian(length, bytes.Length);
			hash.AppendData(length);
			hash.AppendData(bytes);
		}

		private static void AppendFile(IncrementalHash hash, string path)
		{
			using FileStream stream = new FileStream(
				path,
				FileMode.Open,
				FileAccess.Read,
				FileShare.ReadWrite | FileShare.Delete,
				bufferSize: 64 * 1024,
				FileOptions.SequentialScan);
			Span<byte> length = stackalloc byte[sizeof(long)];
			BinaryPrimitives.WriteInt64LittleEndian(length, stream.Length);
			hash.AppendData(length);

			byte[] buffer = new byte[64 * 1024];
			int read;
			while ((read = stream.Read(buffer, 0, buffer.Length)) != 0)
			{
				hash.AppendData(buffer, 0, read);
			}
		}
	}
}
