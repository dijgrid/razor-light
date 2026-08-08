using System;
using System.Threading;
using Xunit;

namespace RazorLight.Precompile.Tests
{
	public class CancellationTests
	{
		[Fact]
		public void Program_Observes_Cancellation_Before_Command_Start()
		{
			using var cancellationSource = new CancellationTokenSource();
			cancellationSource.Cancel();

			Assert.Throws<OperationCanceledException>(() =>
				Program.DoRun(new[] { "help" }, cancellationSource.Token));
		}

		[Fact]
		public void Precompile_Command_Observes_Cancellation_Before_Parsing()
		{
			using var cancellationSource = new CancellationTokenSource();
			cancellationSource.Cancel();

			Assert.Throws<OperationCanceledException>(() =>
				new PrecompileCmd().Run(Array.Empty<string>(), cancellationSource.Token));
		}
	}
}
