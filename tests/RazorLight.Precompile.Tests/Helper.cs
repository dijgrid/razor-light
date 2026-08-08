using System.Text;
using Xunit;

namespace RazorLight.Precompile.Tests
{
	internal static class Helper
	{
		public static string RunCommandTrimNewline(params string[] args)
		{
			var sb = RunCommand(args);
			sb.Replace("\r", "").Replace("\n", "");
			return sb.ToString();
		}

		public static StringBuilder RunCommand(params string[] args)
		{
			var sw = new StringWriter();
			Program.ConsoleOut = sw;
			var exitCode = Program.DoRun(args);
			Assert.Equal(0, exitCode);
			sw.Close();

			return sw.GetStringBuilder();
		}
	}
}
