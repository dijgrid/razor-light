using System;
using System.IO;
using System.Text;

namespace RazorLight.Razor
{
	internal static class FileSystemRazorProjectHelper
	{
		private static readonly StringComparison PathComparison = OperatingSystem.IsWindows()
			? StringComparison.OrdinalIgnoreCase
			: StringComparison.Ordinal;

		public static string NormalizeKey(string templateKey)
		{
			if (string.IsNullOrEmpty(templateKey))
			{
				throw new ArgumentNullException(nameof(templateKey));
			}

			var addLeadingSlash = templateKey[0] != '\\' && templateKey[0] != '/';
			var transformSlashes = templateKey.IndexOf('\\') != -1;

			if (!addLeadingSlash && !transformSlashes)
			{
				return templateKey;
			}

			var length = templateKey.Length;
			if (addLeadingSlash)
			{
				length++;
			}

			var builder = new StringBuilder(length);
			if (addLeadingSlash)
			{
				builder.Append('/');
			}

			for (var i = 0; i < templateKey.Length; i++)
			{
				var ch = templateKey[i];
				if (ch == '\\')
				{
					ch = '/';
				}
				builder.Append(ch);
			}

			return builder.ToString();
		}

		public static string NormalizeRoot(string root)
		{
			if (string.IsNullOrWhiteSpace(root))
			{
				throw new ArgumentException("A file-system root is required.", nameof(root));
			}

			return Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
		}

		public static string ResolveContainedPath(string root, string key, string description)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentException($"A {description} is required.", nameof(key));
			}

			string normalizedRoot = NormalizeRoot(root);
			string relativeKey = key
				.Replace('\\', Path.DirectorySeparatorChar)
				.Replace('/', Path.DirectorySeparatorChar)
				.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			string candidate = Path.GetFullPath(Path.Combine(normalizedRoot, relativeKey));
			string rootPrefix = Path.EndsInDirectorySeparator(normalizedRoot)
				? normalizedRoot
				: normalizedRoot + Path.DirectorySeparatorChar;

			if (!string.Equals(candidate, normalizedRoot, PathComparison) &&
				!candidate.StartsWith(rootPrefix, PathComparison))
			{
				throw new InvalidOperationException(
					$"The {description} '{key}' must remain inside the configured root '{normalizedRoot}'.");
			}

			return candidate;
		}
	}
}
