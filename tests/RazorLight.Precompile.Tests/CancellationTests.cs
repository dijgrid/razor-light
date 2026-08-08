using System;
using System.Threading;
using Xunit;

namespace RazorLight.Precompile.Tests
{
	public class CancellationTests
	{
		[Fact]
		public async Task Program_Observes_Cancellation_Before_Command_Start()
		{
			using var cancellationSource = new CancellationTokenSource();
			cancellationSource.Cancel();

			await Assert.ThrowsAsync<OperationCanceledException>(() =>
				Program.DoRunAsync(new[] { "help" }, cancellationSource.Token));
		}

		[Fact]
		public async Task Precompile_Command_Observes_Cancellation_Before_Parsing()
		{
			using var cancellationSource = new CancellationTokenSource();
			cancellationSource.Cancel();

			await Assert.ThrowsAsync<OperationCanceledException>(() =>
				new PrecompileCmd().RunAsync(Array.Empty<string>(), cancellationSource.Token));
		}
	}
}
