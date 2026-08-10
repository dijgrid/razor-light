using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;

namespace RazorLight.Generation
{
	/// <summary>Indicates that Razor parsing or C# source generation failed.</summary>
	public class TemplateGenerationException : RazorLightException
	{
		/// <summary>Creates a generation failure from Razor diagnostics.</summary>
		public TemplateGenerationException(string message, IReadOnlyList<RazorDiagnostic> diagnostic) : base(message)
		{
			Diagnostics = diagnostic;
		}

		/// <summary>Gets or sets the Razor diagnostics that caused generation to fail.</summary>
		public IReadOnlyList<RazorDiagnostic> Diagnostics { get; set; }
	}
}
