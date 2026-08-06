using RazorLight;
using System;
using System.Collections.Generic;

namespace RazorLight.Precompile
{
	internal sealed class CommandLineArguments
	{
		private readonly Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
		private readonly HashSet<string> flags = new(StringComparer.OrdinalIgnoreCase);

		private CommandLineArguments()
		{
		}

		public static CommandLineArguments Parse(
			string[] args,
			IEnumerable<string> valueOptions,
			IEnumerable<string> flagOptions = null)
		{
			var allowedValues = new HashSet<string>(valueOptions, StringComparer.OrdinalIgnoreCase);
			var allowedFlags = new HashSet<string>(flagOptions ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
			var result = new CommandLineArguments();

			for (var index = 0; index < args.Length; index++)
			{
				var token = args[index];
				var separator = token.IndexOf('=');
				var option = separator >= 0 ? token.Substring(0, separator) : token;

				if (allowedFlags.Contains(option))
				{
					if (separator >= 0)
					{
						throw new RazorLightException($"Option {option} does not accept a value.");
					}

					result.flags.Add(option);
					continue;
				}

				if (!allowedValues.Contains(option))
				{
					throw new RazorLightException($"Unrecognized command line option {option}.");
				}

				string value;
				if (separator >= 0)
				{
					value = token.Substring(separator + 1);
				}
				else
				{
					if (++index >= args.Length)
					{
						throw new RazorLightException($"Option {option} requires a value.");
					}

					value = args[index];
				}

				result.values[option] = value;
			}

			return result;
		}

		public string GetRequiredValue(params string[] aliases)
		{
			var value = GetValue(aliases);
			if (string.IsNullOrWhiteSpace(value))
			{
				throw new RazorLightException($"Required option {aliases[aliases.Length - 1]} was not provided.");
			}

			return value;
		}

		public string GetValue(params string[] aliases)
		{
			foreach (var alias in aliases)
			{
				if (values.TryGetValue(alias, out var value))
				{
					return value;
				}
			}

			return null;
		}

		public bool HasFlag(params string[] aliases)
		{
			foreach (var alias in aliases)
			{
				if (flags.Contains(alias))
				{
					return true;
				}
			}

			return false;
		}
	}
}
