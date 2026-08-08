using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace RazorLight.Precompile
{
	public class Program
	{
		public static TextWriter ConsoleOut { get; set; } = Console.Out;

		public static int Main(string[] args)
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
				return DoRun(args, cancellationSource.Token);
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

		public static int DoRun(string[] args) => DoRun(args, CancellationToken.None);

		public static int DoRun(string[] args, CancellationToken cancellationToken)
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
					return new PrecompileCmd().Run(commandArgs, cancellationToken);
				case "render":
					return new RenderCmd().Run(commandArgs, cancellationToken);
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
