using Microsoft.CodeAnalysis;

namespace RazorLight.Compilation
{
	/// <summary>Describes one mapped C# compiler diagnostic for a generated template.</summary>
	public sealed class TemplateCompilationDiagnostic
	{
		/// <summary>Gets the compiler's concise error text.</summary>
		public string ErrorMessage { get; }
		/// <summary>Gets the complete formatted compiler diagnostic.</summary>
		public string FormattedMessage { get; }
		/// <summary>Gets the mapped source location when one is available.</summary>
		public FileLinePositionSpan? LineSpan { get; }

		/// <summary>Creates a structured template compilation diagnostic.</summary>
		public TemplateCompilationDiagnostic(string errorMessage, string formattedMessage, FileLinePositionSpan? lineSpan)
		{
			ErrorMessage = errorMessage;
			FormattedMessage = formattedMessage;
			LineSpan = lineSpan;
		}
	}
}
