using RazorLight.Compilation;
using RazorLight.Razor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RazorLight.Generation
{
	internal sealed class CSharpSourceResolver
	{
		private readonly RazorLightProject? project;
		private readonly RazorLightOptions options;
		private readonly bool includeDetailedDiagnostics;

		public CSharpSourceResolver(RazorLightProject? project, RazorLightOptions options, bool includeDetailedDiagnostics)
		{
			this.project = project;
			this.options = options;
			this.includeDetailedDiagnostics = includeDetailedDiagnostics;
		}

		public async Task<IReadOnlyList<CSharpSourceDocument>> ResolveAsync(
			RazorLightProjectItem template,
			IEnumerable<string> directivePaths)
		{
			var result = new List<CSharpSourceDocument>();
			var seen = new HashSet<string>(StringComparer.Ordinal);

			foreach (string sourceKey in options.CSharpSourceKeys)
			{
				await AddAsync(sourceKey, template, isRelative: false, seen, result).ConfigureAwait(false);
			}

			foreach (string sourceKey in directivePaths)
			{
				await AddAsync(sourceKey, template, isRelative: true, seen, result).ConfigureAwait(false);
			}

			return result;
		}

		private async Task AddAsync(
			string sourceKey,
			RazorLightProjectItem template,
			bool isRelative,
			ISet<string> seen,
			ICollection<CSharpSourceDocument> result)
		{
			string normalizedKey = Normalize(sourceKey, isRelative ? template.Key : null);
			if (!seen.Add(normalizedKey)) return;

			if (options.DynamicCSharpSources.TryGetValue(normalizedKey, out string? content) ||
				options.DynamicCSharpSources.TryGetValue(sourceKey, out content))
			{
				result.Add(new CSharpSourceDocument(normalizedKey, content, expirationToken: null));
				return;
			}

			if (project == null)
			{
				throw CreateResolutionException(normalizedKey, "no RazorLight project is configured");
			}

			RazorLightProjectItem item = await project.GetSourceItemAsync(normalizedKey).ConfigureAwait(false);
			if (!item.Exists)
			{
				throw CreateResolutionException(normalizedKey, "the configured project did not contain it");
			}

			using Stream stream = item.Read();
			using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
			result.Add(new CSharpSourceDocument(normalizedKey, await reader.ReadToEndAsync().ConfigureAwait(false), item.ExpirationToken));
		}

		private InvalidOperationException CreateResolutionException(string sourceKey, string reason)
		{
			string key = includeDetailedDiagnostics ? $" '{sourceKey}'" : string.Empty;
			return new InvalidOperationException(
				$"Could not resolve the requested C# source{key} because {reason}. " +
				$"Enable {nameof(RazorLightOptions)}.{nameof(RazorLightOptions.EnableDebugMode)} for source details.");
		}

		internal static string Normalize(string sourceKey, string? templateKey)
		{
			if (string.IsNullOrWhiteSpace(sourceKey))
				throw new InvalidOperationException("A C# source path cannot be empty.");

			string key = sourceKey.Replace('\\', '/');
			if (!key.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("Only .cs files can be compiled as template sources.");

			string combined;
			if (key.StartsWith("/", StringComparison.Ordinal) || string.IsNullOrEmpty(templateKey))
			{
				combined = key.TrimStart('/');
			}
			else
			{
				string normalizedTemplate = templateKey.Replace('\\', '/');
				int slash = normalizedTemplate.LastIndexOf('/');
				combined = slash < 0 ? key : normalizedTemplate.Substring(0, slash + 1) + key;
			}

			var segments = new List<string>();
			foreach (string segment in combined.Split('/'))
			{
				if (segment.Length == 0 || segment == ".") continue;
				if (segment == "..")
				{
					if (segments.Count == 0)
						throw new InvalidOperationException("C# source paths must remain inside the project root.");
					segments.RemoveAt(segments.Count - 1);
				}
				else
				{
					segments.Add(segment);
				}
			}

			return string.Join("/", segments);
		}
	}
}
