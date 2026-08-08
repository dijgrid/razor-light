using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace RazorLight.Instrumentation
{
	internal static class CompileSourceDirective
	{
		private static readonly object SourcePathsKey = new object();
		public static readonly DirectiveDescriptor Directive = DirectiveDescriptor.CreateDirective(
			"compileSource",
			DirectiveKind.SingleLine,
			builder =>
			{
				builder.AddStringToken("Path", "Project-relative C# source path");
				builder.Usage = DirectiveUsage.FileScopedMultipleOccurring;
				builder.Description = "Compiles a trusted C# source file with the generated template.";
			});

		public static RazorProjectEngineBuilder Register(RazorProjectEngineBuilder builder)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			builder.AddDirective(Directive);
			builder.Features.Add(new Pass());
			return builder;
		}

		public static IReadOnlyList<string> GetSourcePaths(RazorCodeDocument document)
		{
			object? value = document.Items[SourcePathsKey];
			return value is IReadOnlyList<string> paths
				? paths
				: Array.Empty<string>();
		}

		private sealed class Pass : IntermediateNodePassBase, IRazorDirectiveClassifierPass
		{
			public override int Order => int.MinValue;

			protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
			{
				var visitor = new Visitor();
				visitor.Visit(documentNode);
				codeDocument.Items[SourcePathsKey] = visitor.SourcePaths;
			}
		}

		private sealed class Visitor : IntermediateNodeWalker
		{
			public List<string> SourcePaths { get; } = new List<string>();

			public override void VisitDirective(DirectiveIntermediateNode node)
			{
				var tokens = node.Tokens.ToArray();
				if (node.Directive == Directive && tokens.Length > 0)
				{
					SourcePaths.Add(tokens[0].Content.Trim('"'));
				}
			}
		}
	}
}
