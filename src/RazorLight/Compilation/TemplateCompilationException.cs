using System.Collections.Generic;
using System.Linq;

namespace RazorLight.Compilation
{
	/// <summary>Indicates that generated template C# did not compile successfully.</summary>
	public class TemplateCompilationException : RazorLightException
	{
		private readonly List<TemplateCompilationDiagnostic> compilationDiagnostics = new List<TemplateCompilationDiagnostic>();

		/// <summary>Gets concise error messages retained for source compatibility.</summary>
		public IReadOnlyList<string> CompilationErrors => compilationDiagnostics.Select(x => x.FormattedMessage).ToList();

		/// <summary>Gets structured compiler diagnostics including mapped template locations.</summary>
		public IReadOnlyList<TemplateCompilationDiagnostic> CompilationDiagnostics => compilationDiagnostics;

		/// <summary>Creates a compilation exception from structured diagnostics.</summary>
		public TemplateCompilationException(string message, IEnumerable<TemplateCompilationDiagnostic> diagnostics) : base(message)
		{
			if (diagnostics != null)
			{
				compilationDiagnostics.AddRange(diagnostics);
			}
		}
	}
}
