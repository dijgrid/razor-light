using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;

namespace RazorLight.Razor
{
	/// <summary>
	/// Specifies RazorProject where templates are located in files
	/// </summary>
	public sealed class FileSystemRazorProject : RazorLightProject, IDisposable
	{
		public const string DefaultExtension = ".cshtml";
		private readonly IFileProvider _fileProvider;
		private readonly string _normalizedRoot;
		private bool _disposed;

		public FileSystemRazorProject(string root)
			: this(root, DefaultExtension)
		{
		}

		public FileSystemRazorProject(string root, string extension)
		{
			Extension = extension ?? throw new ArgumentNullException(nameof(extension));

			if (!Directory.Exists(root))
			{
				throw new DirectoryNotFoundException($"Root directory {root} not found");
			}

			Root = root;
			_normalizedRoot = FileSystemRazorProjectHelper.NormalizeRoot(root);
			_fileProvider = new PhysicalFileProvider(_normalizedRoot);
		}

		public string Extension { get; set; }

		/// <summary>
		/// Looks up for the template source with a given <paramref name="templateKey" />
		/// </summary>
		/// <param name="templateKey">Unique template key</param>
		/// <returns></returns>
		public override Task<RazorLightProjectItem> GetItemAsync(string templateKey)
			=> GetItemAsync(templateKey, CancellationToken.None);

		public override Task<RazorLightProjectItem> GetItemAsync(string templateKey, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (string.IsNullOrEmpty(templateKey))
			{
				throw new ArgumentNullException(nameof(templateKey));
			}

			if (!templateKey.EndsWith(Extension, StringComparison.Ordinal))
			{
				templateKey = templateKey + Extension;
			}

			string absolutePath = GetAbsoluteFilePathFromKey(templateKey);
			var item = new FileSystemRazorProjectItem(templateKey, new FileInfo(absolutePath));

			if (item.Exists)
			{
				item.ExpirationToken = _fileProvider.Watch(templateKey);
			}

			return Task.FromResult((RazorLightProjectItem)item);
		}

		public override Task<RazorLightProjectItem> GetSourceItemAsync(string sourceKey)
			=> GetSourceItemAsync(sourceKey, CancellationToken.None);

		public override Task<RazorLightProjectItem> GetSourceItemAsync(string sourceKey, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (string.IsNullOrWhiteSpace(sourceKey))
			{
				throw new ArgumentNullException(nameof(sourceKey));
			}

			string relativePath = sourceKey.TrimStart('/', '\\');
			string sourcePath = FileSystemRazorProjectHelper.ResolveContainedPath(
				_normalizedRoot,
				relativePath,
				"C# source path");

			var item = new FileSystemRazorProjectItem(sourceKey, new FileInfo(sourcePath));
			if (item.Exists)
			{
				item.ExpirationToken = _fileProvider.Watch(relativePath.Replace('\\', '/'));
			}

			return Task.FromResult((RazorLightProjectItem)item);
		}

		/// <summary>
		/// Root folder
		/// </summary>
		public string Root { get; }

		private string GetAbsoluteFilePathFromKey(string templateKey)
		{
			if (string.IsNullOrEmpty(templateKey))
			{
				throw new ArgumentNullException(nameof(templateKey));
			}

			return FileSystemRazorProjectHelper.ResolveContainedPath(
				_normalizedRoot,
				templateKey,
				"template path");
		}

		public override Task<IEnumerable<RazorLightProjectItem>> GetImportsAsync(string templateKey)
			=> GetImportsAsync(templateKey, CancellationToken.None);

		public override Task<IEnumerable<RazorLightProjectItem>> GetImportsAsync(string templateKey, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return Task.FromResult(Enumerable.Empty<RazorLightProjectItem>());
		}
		public override Task<IEnumerable<string>> GetKnownKeysAsync()
			=> GetKnownKeysAsync(CancellationToken.None);

		public override Task<IEnumerable<string>> GetKnownKeysAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var files = Directory.EnumerateFiles(_normalizedRoot, $"*{Extension}", SearchOption.AllDirectories)
				.Select(path => Path.GetRelativePath(_normalizedRoot, path).Replace('\\', '/'));

			return Task.FromResult(files);
		}
		public override string NormalizeKey(string templateKey) => FileSystemRazorProjectHelper.NormalizeKey(templateKey);

		internal bool IsDisposed => _disposed;

		public void Dispose()
		{
			if (_disposed) return;
			(_fileProvider as IDisposable)?.Dispose();
			_disposed = true;
		}
	}
}
