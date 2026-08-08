using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using RazorLight.Compilation;
using RazorLight.Instrumentation;
using RazorLight.Razor;

namespace RazorLight.Generation
{
	internal class RazorSourceGenerator
	{
		private readonly bool includeDetailedDiagnostics;
		private readonly RazorLightOptions options;

		public RazorSourceGenerator(RazorEngine projectEngine, RazorLightProject? project = null, ISet<string>? namespaces = null)
			: this(projectEngine, project, namespaces, includeDetailedDiagnostics: false, new RazorLightOptions())
		{
		}

		internal RazorSourceGenerator(
			RazorEngine projectEngine,
			RazorLightProject? project,
			ISet<string>? namespaces,
			bool includeDetailedDiagnostics,
			RazorLightOptions? options = null)
		{
			if (projectEngine == null)
			{
				throw new ArgumentNullException(nameof(projectEngine));
			}

			Namespaces = namespaces ?? new HashSet<string>();
			this.includeDetailedDiagnostics = includeDetailedDiagnostics;
			this.options = options ?? new RazorLightOptions();

			ProjectEngine = projectEngine;
			Project = project;
			DefaultImports = GetDefaultImports();
		}

		public RazorEngine ProjectEngine { get; set; }

		public RazorLightProject? Project { get; set; }

		public ISet<string> Namespaces { get; set; }

		public RazorSourceDocument DefaultImports { get; set; }

		/// <summary>
		/// Parses the template specified by the project item <paramref name="key"/>.
		/// </summary>
		/// <param name="key">The template path.</param>
		/// <returns>The <see cref="IGeneratedRazorTemplate"/>.</returns>
		public Task<IGeneratedRazorTemplate> GenerateCodeAsync(string key) =>
			GenerateCodeAsync(key, CancellationToken.None);

		public async Task<IGeneratedRazorTemplate> GenerateCodeAsync(string key, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (string.IsNullOrEmpty(key))
			{
				throw new ArgumentException();
			}

			if (Project == null)
			{
				string templateDescription = includeDetailedDiagnostics ? $" the template \"{key}\"" : " the requested template";
				string _message = $"Can not resolve a content for{templateDescription} as there is no project set. " +
					"You can only render a template by passing it's content directly via string using corresponding function overload";

				throw new InvalidOperationException(_message);
			}

			RazorLightProjectItem projectItem = await Project.GetItemAsync(key, cancellationToken).ConfigureAwait(false);
			return await GenerateCodeAsync(projectItem, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Parses the template specified by <paramref name="projectItem"/>.
		/// </summary>
		/// <param name="projectItem">The <see cref="RazorLightProjectItem"/>.</param>
		/// <returns>The <see cref="IGeneratedRazorTemplate"/>.</returns>
		public Task<IGeneratedRazorTemplate> GenerateCodeAsync(RazorLightProjectItem projectItem) =>
			GenerateCodeAsync(projectItem, CancellationToken.None);

		public Task<IGeneratedRazorTemplate> GenerateCodeAsync(
			RazorLightProjectItem projectItem,
			CancellationToken cancellationToken) =>
			GenerateCodeAsync(projectItem, modelType: null, cancellationToken);

		internal Task<IGeneratedRazorTemplate> GenerateCodeAsync(
			RazorLightProjectItem projectItem,
			Type? modelType) =>
			GenerateCodeAsync(projectItem, modelType, CancellationToken.None);

		internal async Task<IGeneratedRazorTemplate> GenerateCodeAsync(
			RazorLightProjectItem projectItem,
			Type? modelType,
			CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (projectItem == null)
			{
				throw new ArgumentNullException(nameof(projectItem));
			}

			if (!projectItem.Exists)
			{
				throw CreateMissingProjectItemException(projectItem);
			}

			RazorCodeDocument codeDocument = await CreateCodeDocumentAsync(projectItem, modelType, cancellationToken).ConfigureAwait(false);
			cancellationToken.ThrowIfCancellationRequested();
			ProjectEngine.Process(codeDocument);
			cancellationToken.ThrowIfCancellationRequested();

			RazorCSharpDocument document = codeDocument.GetCSharpDocument();
			if (document.Diagnostics.Count > 0)
			{
				var builder = new StringBuilder();
				builder.AppendLine("Failed to generate Razor template. See \"Diagnostics\" property for more details");

				foreach (RazorDiagnostic d in document.Diagnostics)
				{
					builder.AppendLine(includeDetailedDiagnostics
						? $"- {d.GetMessage()}"
						: $"- Razor diagnostic {d.Id} at line {d.Span.LineIndex}, character {d.Span.CharacterIndex}. " +
						  $"Enable {nameof(RazorLightOptions)}.{nameof(RazorLightOptions.EnableDebugMode)} for details.");
				}

				throw new TemplateGenerationException(
					builder.ToString(),
					includeDetailedDiagnostics ? document.Diagnostics : Array.Empty<RazorDiagnostic>());
			}

			IReadOnlyList<string> sourcePaths = CompileSourceDirective.GetSourcePaths(codeDocument);
			var resolver = new CSharpSourceResolver(Project, options, includeDetailedDiagnostics);
			IReadOnlyList<CSharpSourceDocument> sources = await resolver.ResolveAsync(projectItem, sourcePaths, cancellationToken).ConfigureAwait(false);

			return new GeneratedRazorTemplate(projectItem, document, sources);
		}

		/// <summary>
		/// Generates a <see cref="RazorCodeDocument"/> for the specified <paramref name="projectItem"/>.
		/// </summary>
		/// <param name="projectItem">The <see cref="RazorLightProjectItem"/>.</param>
		/// <returns>The created <see cref="RazorCodeDocument"/>.</returns>
		public virtual Task<RazorCodeDocument> CreateCodeDocumentAsync(RazorLightProjectItem projectItem) =>
			CreateCodeDocumentAsync(projectItem, modelType: null, CancellationToken.None);

		internal Task<RazorCodeDocument> CreateCodeDocumentAsync(
			RazorLightProjectItem projectItem,
			Type? modelType) =>
			CreateCodeDocumentAsync(projectItem, modelType, CancellationToken.None);

		internal async Task<RazorCodeDocument> CreateCodeDocumentAsync(
			RazorLightProjectItem projectItem,
			Type? modelType,
			CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (projectItem == null)
			{
				throw new ArgumentNullException(nameof(projectItem));
			}

			if (!projectItem.Exists)
			{
				throw CreateMissingProjectItemException(projectItem);
			}

			using (var stream = projectItem.Read())
			{
				RazorSourceDocument source = RazorSourceDocument.ReadFrom(stream, projectItem.Key);
				var imports = (await GetImportsAsync(projectItem, cancellationToken).ConfigureAwait(false)).ToList();
				RejectTagHelperDirectives(source);
				foreach (var import in imports)
				{
					RejectTagHelperDirectives(import);
				}
				if (modelType != null)
				{
					imports.Add(GetModelImport(modelType));
				}

				return RazorCodeDocument.Create(source, imports);
			}
		}

		private static void RejectTagHelperDirectives(RazorSourceDocument source)
		{
			var characters = new char[source.Length];
			source.CopyTo(0, characters, 0, characters.Length);
			string content = new string(characters);
			foreach (string line in content.Split('\n'))
			{
				string directive = line.TrimStart();
				if (IsDirective(directive, "@addTagHelper") ||
					IsDirective(directive, "@removeTagHelper") ||
					IsDirective(directive, "@tagHelperPrefix"))
				{
					throw new TemplateGenerationException(
						"Tag helpers are not supported by the generic RazorLight core. Remove the tag-helper directive.",
						Array.Empty<RazorDiagnostic>());
				}
			}
		}

		private static bool IsDirective(string line, string directive) =>
			line.Equals(directive, StringComparison.Ordinal) ||
			line.StartsWith(directive + " ", StringComparison.Ordinal) ||
			line.StartsWith(directive + "\t", StringComparison.Ordinal);

		private InvalidOperationException CreateMissingProjectItemException(RazorLightProjectItem projectItem)
		{
			string message = $"{nameof(RazorLightProjectItem)} of type {projectItem.GetType().FullName} does not exist.";
			if (includeDetailedDiagnostics)
			{
				message = $"{nameof(RazorLightProjectItem)} of type {projectItem.GetType().FullName} with key {projectItem.Key} does not exist.";
			}

			return new InvalidOperationException(message);
		}

		/// <summary>
		/// Gets <see cref="RazorSourceDocument"/> that are applicable to the specified <paramref name="projectItem"/>.
		/// </summary>
		/// <param name="projectItem">The <see cref="RazorLightProjectItem"/>.</param>
		/// <returns>The sequence of applicable <see cref="RazorSourceDocument"/>.</returns>
		public virtual Task<IEnumerable<RazorSourceDocument>> GetImportsAsync(RazorLightProjectItem projectItem) =>
			GetImportsAsync(projectItem, CancellationToken.None);

		public virtual async Task<IEnumerable<RazorSourceDocument>> GetImportsAsync(
			RazorLightProjectItem projectItem,
			CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (projectItem == null)
			{
				throw new ArgumentNullException(nameof(projectItem));
			}

			var result = new List<RazorSourceDocument>();

			if (Project != null && projectItem is not TextSourceRazorProjectItem)
			{
				IEnumerable<RazorLightProjectItem> importProjectItems = await Project.GetImportsAsync(projectItem.Key, cancellationToken).ConfigureAwait(false);
				foreach (var importItem in importProjectItems)
				{
					cancellationToken.ThrowIfCancellationRequested();
					if (importItem.Exists)
					{
						using (var stream = importItem.Read())
						{
							result.Insert(0, RazorSourceDocument.ReadFrom(stream, null));
						}
					}
				}
			}

			if (Namespaces.Count > 0)
			{
				RazorSourceDocument namespacesImports = GetNamespacesImports();
				if (namespacesImports != null)
				{
					result.Insert(0, namespacesImports);
				}
			}

			if (DefaultImports != null)
			{
				result.Insert(0, DefaultImports);
			}

			return result;
		}

		private static RazorSourceDocument GetModelImport(Type modelType)
		{
			var modelTypeInfo = new ModelTypeInfo(modelType);
			if (!modelTypeInfo.IsStrongType || !modelType.IsVisible || modelType.ContainsGenericParameters)
			{
				throw new ArgumentException(
					$"The explicit model type '{modelType}' must be a visible, closed, non-anonymous type.",
					nameof(modelType));
			}

			string content = $"@model {modelTypeInfo.TemplateTypeName}";
			return RazorSourceDocument.Create(content, fileName: null, encoding: Encoding.UTF8);
		}

		internal protected RazorSourceDocument GetDefaultImports()
		{
			using (var stream = new MemoryStream())
			using (var writer = new StreamWriter(stream, Encoding.UTF8))
			{
				foreach (string line in GetDefaultImportLines())
				{
					writer.WriteLine(line);
				}

				writer.Flush();

				stream.Position = 0;
				return RazorSourceDocument.ReadFrom(stream, fileName: null, encoding: Encoding.UTF8);
			}
		}

		internal protected RazorSourceDocument GetNamespacesImports()
		{
			using (var stream = new MemoryStream())
			using (var writer = new StreamWriter(stream, Encoding.UTF8))
			{
				foreach (string @namespace in Namespaces)
				{
					writer.WriteLine($"@using {@namespace}");
				}

				writer.Flush();

				stream.Position = 0;
				return RazorSourceDocument.ReadFrom(stream, fileName: null, encoding: Encoding.UTF8);
			}
		}

		public virtual IEnumerable<string> GetDefaultImportLines()
		{
			yield return "@using System";
			yield return "@using System.Collections.Generic";
			yield return "@using System.Linq";
			yield return "@using System.Threading.Tasks";
		}
	}
}
