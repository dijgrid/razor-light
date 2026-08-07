using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RazorLight.Compilation
{
	internal sealed class TemplateCompilationRequest
	{
		private TemplateCompilationRequest(
			string templateKey,
			string? templateContent,
			Type? modelType,
			IEnumerable<string> namespaces)
		{
			TemplateKey = templateKey;
			TemplateContent = templateContent;
			ModelType = modelType;
			CacheKey = CreateCacheKey(templateKey, templateContent, modelType, namespaces);
		}

		public string TemplateKey { get; }

		public string? TemplateContent { get; }

		public Type? ModelType { get; }

		public string CacheKey { get; }

		public bool IsStringTemplate => TemplateContent != null;

		public static TemplateCompilationRequest ForProject(
			string templateKey,
			Type? modelType,
			IEnumerable<string> namespaces)
		{
			return new TemplateCompilationRequest(templateKey, null, modelType, namespaces);
		}

		public static TemplateCompilationRequest ForString(
			string templateKey,
			string templateContent,
			Type? modelType,
			IEnumerable<string> namespaces)
		{
			return new TemplateCompilationRequest(templateKey, templateContent, modelType, namespaces);
		}

		private static string CreateCacheKey(
			string templateKey,
			string? templateContent,
			Type? modelType,
			IEnumerable<string> namespaces)
		{
			if (templateContent == null && modelType == null)
			{
				return templateKey;
			}

			var identity = new StringBuilder()
				.AppendLine(templateContent == null ? "project" : "string")
				.AppendLine(templateKey)
				.AppendLine(templateContent ?? string.Empty)
				.AppendLine(modelType?.AssemblyQualifiedName ?? "dynamic");

			foreach (string @namespace in namespaces.OrderBy(value => value, StringComparer.Ordinal))
			{
				identity.AppendLine(@namespace);
			}

			byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(identity.ToString()));
			return templateKey + ".__razorlight." + Convert.ToHexString(hash).ToLowerInvariant();
		}
	}
}
