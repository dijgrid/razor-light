using Microsoft.AspNetCore.Razor.Language;
using RazorLight.Compilation;
using RazorLight.Razor;
using System;
using System.Collections.Generic;

namespace RazorLight.Generation
{
	public class GeneratedRazorTemplate : IGeneratedRazorTemplate, IGeneratedCSharpSourceContainer
	{
		public GeneratedRazorTemplate(RazorLightProjectItem projectItem, RazorCSharpDocument cSharpDocument)
			: this(projectItem, cSharpDocument, Array.Empty<CSharpSourceDocument>())
		{
		}

		internal GeneratedRazorTemplate(
			RazorLightProjectItem projectItem,
			RazorCSharpDocument cSharpDocument,
			IReadOnlyList<CSharpSourceDocument> cSharpSources)
		{
			ProjectItem = projectItem ?? throw new ArgumentNullException(nameof(projectItem));
			CSharpDocument = cSharpDocument ?? throw new ArgumentNullException(nameof(cSharpDocument));
			CSharpSources = cSharpSources ?? throw new ArgumentNullException(nameof(cSharpSources));
		}

		public RazorLightProjectItem ProjectItem { get; set; }

		public string TemplateKey => ProjectItem.Key;

		public RazorCSharpDocument CSharpDocument { get; set; }

		public string GeneratedCode => CSharpDocument.GeneratedCode;

		IReadOnlyList<CSharpSourceDocument> IGeneratedCSharpSourceContainer.CSharpSources => CSharpSources;

		internal IReadOnlyList<CSharpSourceDocument> CSharpSources { get; }
	}
}
