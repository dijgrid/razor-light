using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace RazorLight.Razor
{
	/// <inheritdoc />
	public sealed class EmbeddedRazorProject : RazorLightProject
	{
		/// <inheritdoc />
		public EmbeddedRazorProject(Type rootType)
		{
			if (rootType == null)
			{
				throw new ArgumentNullException(nameof(rootType));
			}

			Assembly = rootType.Assembly;
			RootNamespace = rootType.Namespace ?? string.Empty;
		}

		/// <inheritdoc />
		public EmbeddedRazorProject(Assembly assembly, string? rootNamespace = "")
		{
			Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));

			RootNamespace = rootNamespace ?? string.Empty;
		}

		/// <inheritdoc />
		public Assembly Assembly { get; set; }

		/// <inheritdoc />
		public string RootNamespace { get; set; }

		/// <inheritdoc />
		public string Extension { get; set; } = ".cshtml";

		/// <inheritdoc />
		public override Task<RazorLightProjectItem> GetItemAsync(string templateKey)
			=> GetItemAsync(templateKey, CancellationToken.None);

		/// <inheritdoc />
		public override Task<RazorLightProjectItem> GetItemAsync(string templateKey, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (string.IsNullOrEmpty(templateKey))
			{
				throw new ArgumentNullException(nameof(templateKey));
			}

			if (!templateKey.EndsWith(Extension, StringComparison.Ordinal))
			{
				templateKey += Extension;
			}

			var item = new EmbeddedRazorProjectItem(Assembly, RootNamespace, templateKey);

			return Task.FromResult((RazorLightProjectItem)item);
		}

		/// <inheritdoc />
		public override Task<RazorLightProjectItem> GetSourceItemAsync(string sourceKey)
			=> GetSourceItemAsync(sourceKey, CancellationToken.None);

		/// <inheritdoc />
		public override Task<RazorLightProjectItem> GetSourceItemAsync(string sourceKey, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (string.IsNullOrWhiteSpace(sourceKey))
			{
				throw new ArgumentNullException(nameof(sourceKey));
			}

			string resourceKey = sourceKey.TrimStart('/', '\\').Replace('/', '.').Replace('\\', '.');
			return Task.FromResult<RazorLightProjectItem>(
				new EmbeddedRazorProjectItem(Assembly, RootNamespace, resourceKey));
		}

		/// <inheritdoc />
		public override Task<IEnumerable<RazorLightProjectItem>> GetImportsAsync(string templateKey)
			=> GetImportsAsync(templateKey, CancellationToken.None);

		/// <inheritdoc />
		public override Task<IEnumerable<RazorLightProjectItem>> GetImportsAsync(string templateKey, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return Task.FromResult(Enumerable.Empty<RazorLightProjectItem>());
		}

		/// <inheritdoc />
		public override Task<IEnumerable<string>> GetKnownKeysAsync()
			=> GetKnownKeysAsync(CancellationToken.None);

		/// <inheritdoc />
		public override Task<IEnumerable<string>> GetKnownKeysAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var ignoredPrefix = string.IsNullOrEmpty(RootNamespace) ? Assembly.GetName().FullName ?? string.Empty : RootNamespace;
			if (!ignoredPrefix.EndsWith(".")) ignoredPrefix += ".";

			var fullResourceNames = this.Assembly.GetManifestResourceNames()
				.Where(x => x.StartsWith(ignoredPrefix, StringComparison.Ordinal) &&
					x.EndsWith(Extension, StringComparison.Ordinal));

			var keys = fullResourceNames
				.Select(x => x.Remove(0, ignoredPrefix.Length)) // Remove prefix
				.Select(x => x.Remove(x.Length - Extension.Length, Extension.Length)); // Remove extension

			return Task.FromResult(keys);
		}
	}
}
