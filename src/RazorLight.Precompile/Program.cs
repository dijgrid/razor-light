using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RazorLight.Precompile
{
	public class Program
	{
		public static TextWriter ConsoleOut { get; set; } = Console.Out;

		public static async Task<int> Main(string[] args)
		{
			using var cancellationSource = new CancellationTokenSource();
			ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
			{
				eventArgs.Cancel = true;
				cancellationSource.Cancel();
			};
			Console.CancelKeyPress += cancelHandler;
			try
			{
				return await DoRunAsync(args, cancellationSource.Token).ConfigureAwait(false);
			}
			catch (OperationCanceledException) when (cancellationSource.IsCancellationRequested)
			{
				return 130;
			}
			catch (Exception exc)
			{
				Console.Error.WriteLine(exc);
				return 1;
			}
			finally
			{
				Console.CancelKeyPress -= cancelHandler;
			}
		}

		public static Task<int> DoRunAsync(string[] args) => DoRunAsync(args, CancellationToken.None);

		public static async Task<int> DoRunAsync(string[] args, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (args == null || args.Length == 0)
			{
				WriteUsage();
				return 1;
			}

			var commandArgs = args.Skip(1).ToArray();
			switch (args[0].ToLowerInvariant())
			{
				case "precompile":
					return await new PrecompileCmd().RunAsync(commandArgs, cancellationToken).ConfigureAwait(false);
				case "render":
					return await new RenderCmd().RunAsync(commandArgs, cancellationToken).ConfigureAwait(false);
				case "help":
				case "--help":
				case "-h":
					WriteUsage();
					return 0;
				default:
					throw new RazorLightException($"Unknown command {args[0]}.");
			}
		}

		private static void WriteUsage()
		{
			ConsoleOut.WriteLine("Usage: razorlight-precompile <precompile|render> [options]");
		}
	}
}
