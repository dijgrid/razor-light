using System;
using System.IO;
using System.Linq;

namespace RazorLight.Precompile
{
	public class Program
	{
		public static TextWriter ConsoleOut { get; set; } = Console.Out;

		public static int Main(string[] args)
		{
			try
			{
				return DoRun(args);
			}
			catch (Exception exc)
			{
				Console.Error.WriteLine(exc);
				return 1;
			}
		}

		public static int DoRun(string[] args)
		{
			if (args == null || args.Length == 0)
			{
				WriteUsage();
				return 1;
			}

			var commandArgs = args.Skip(1).ToArray();
			switch (args[0].ToLowerInvariant())
			{
				case "precompile":
					return new PrecompileCmd().Run(commandArgs);
				case "render":
					return new RenderCmd().Run(commandArgs);
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
