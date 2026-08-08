using System.Collections.Generic;
using System.Linq;

namespace RazorLight.Compilation
{
	public class TemplateCompilationException : RazorLightException
	{
		private readonly List<TemplateCompilationDiagnostic> compilationDiagnostics = new List<TemplateCompilationDiagnostic>();

		public IReadOnlyList<string> CompilationErrors => compilationDiagnostics.Select(x => x.FormattedMessage).ToList();

		public IReadOnlyList<TemplateCompilationDiagnostic> CompilationDiagnostics => compilationDiagnostics;

		public TemplateCompilationException(string message, IEnumerable<TemplateCompilationDiagnostic> diagnostics) : base(message)
		{
			if (diagnostics != null)
			{
				compilationDiagnostics.AddRange(diagnostics);
			}
		}
	}
}
