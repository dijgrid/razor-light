using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileSystemGlobbing;
using Mono.Cecil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RazorLight.Caching;

namespace RazorLight.Precompile
{
	internal class RenderCmd
	{
		public Task<int> RunAsync(string[] args) => RunAsync(args, CancellationToken.None);

		public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var options = CommandLineArguments.Parse(
				args,
				new[] { "-p", "--path", "-m", "--model", "-k", "--key", "-q", "--jsonQuery", "-l", "--log" },
				new[] { "-r", "--recurse" });
			var path = options.GetRequiredValue("-p", "--path");
			var modelFilePath = options.GetRequiredValue("-m", "--model");
			var key = options.GetValue("-k", "--key");
			var jsonQuery = options.GetValue("-q", "--jsonQuery");
			var logFilePath = options.GetValue("-l", "--log");
			var searchOption = options.HasFlag("-r", "--recurse")
				? SearchOption.AllDirectories
				: SearchOption.TopDirectoryOnly;

			var modelToken = JsonConvert.DeserializeObject<JToken>(
				await File.ReadAllTextAsync(modelFilePath, cancellationToken).ConfigureAwait(false));
			if (jsonQuery != null)
			{
				modelToken = modelToken?.SelectToken(jsonQuery);
			}

			var model = JsonModel.New(modelToken);

			await using var log = logFilePath == null ? null : new StreamWriter(logFilePath);
			using var cachingProvider = await PrecompiledCachingProvider
				.CreateAsync(YieldFiles(path, searchOption), log, cancellationToken)
				.ConfigureAwait(false);

			if (key == null)
			{
				if (cachingProvider.Map.Count > 1)
				{
					throw new RazorLightException($"Found {cachingProvider.Map.Count} precompiled templates and no --key argument was given.");
				}

				key = cachingProvider.Map.First().Key;
			}
			else if (key[0] != '/')
			{
				key = '/' + key;
			}

			using var engine = RazorLightEngineBuilder.CreatePrecompiled(cachingProvider);

			if (!cachingProvider.TryGetTemplate(key, out var pageFactory))
			{
				throw new RazorLightException($"No precompiled template found for the key {key}");
			}

			var templatePage = pageFactory();
			Program.ConsoleOut.WriteLine(
				await engine.RenderTemplateAsync(templatePage, model, cancellationToken).ConfigureAwait(false));
			return 0;
		}

		private static IEnumerable<string> YieldFiles(string path, SearchOption searchOption)
		{
			if (path.Contains(','))
			{
				return path.Split(',').SelectMany(DoYieldFiles);
			}

			return DoYieldFiles(path);

			IEnumerable<string> DoYieldFiles(string fileOrFolderPath)
			{
				if (fileOrFolderPath.Contains('*') || fileOrFolderPath.Contains('?') || fileOrFolderPath.Contains('['))
				{
					var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
					matcher.AddInclude(fileOrFolderPath.Replace('\\', '/'));
					return matcher.GetResultsInFullPath(Directory.GetCurrentDirectory());
				}

				if (File.Exists(fileOrFolderPath))
				{
					if (fileOrFolderPath.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase))
					{
						return new[] { fileOrFolderPath };
					}

					throw new RazorLightException($"{fileOrFolderPath} is not a valid precompiled template assembly.");
				}

				if (Directory.Exists(fileOrFolderPath))
				{
					return Directory.EnumerateFiles(fileOrFolderPath, "*.dll", searchOption);
				}

				throw new RazorLightException($"{fileOrFolderPath} is not found.");
			}
		}
	}
}
