using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RazorLight.Compilation;
using Xunit;

namespace RazorLight.Tests.Compilation
{
	public class TemplateCompilationExceptionTests
	{
		[Fact]
		public void Ensure_CompilationDiagnostics_FormattedMessage_Matches_CompilationErrors()
		{
			var exception = new TemplateCompilationException("Error message", new TemplateCompilationDiagnostic[]
			{
				new TemplateCompilationDiagnostic("diagnosticMessage", "diagnosticFormattedMessage",
					new FileLinePositionSpan("path", new LinePosition(3, 1), new LinePosition(4, 2)))
			});
			
			Assert.NotEmpty(exception.CompilationDiagnostics);
			Assert.NotEmpty(exception.CompilationErrors);
			
			var firstDiagnostic = Assert.Single(exception.CompilationDiagnostics);
			Assert.Single(exception.CompilationErrors);
			Assert.Equal("diagnosticMessage",firstDiagnostic.ErrorMessage);
			Assert.Equal("diagnosticFormattedMessage",firstDiagnostic.FormattedMessage);
			Assert.Equal("path",firstDiagnostic.LineSpan?.Path);
			Assert.Equal(3,firstDiagnostic.LineSpan?.StartLinePosition.Line);
			Assert.Equal(1,firstDiagnostic.LineSpan?.StartLinePosition.Character);
			Assert.Equal(4,firstDiagnostic.LineSpan?.EndLinePosition.Line);
			Assert.Equal(2,firstDiagnostic.LineSpan?.EndLinePosition.Character);
			
			Assert.Equal(exception.CompilationErrors[0], firstDiagnostic.FormattedMessage);
		}
	}
}
