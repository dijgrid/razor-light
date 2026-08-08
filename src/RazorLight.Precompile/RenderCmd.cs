using Microsoft.Extensions.FileSystemGlobbing;
using Mono.Cecil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RazorLight.Caching;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RazorLight.Precompile
{
	internal class RenderCmd
	{
		public int Run(string[] args)
		{
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

			var modelToken = JsonConvert.DeserializeObject<JToken>(File.ReadAllText(modelFilePath));
			if (jsonQuery != null)
			{
				modelToken = modelToken?.SelectToken(jsonQuery);
			}

			var model = JsonModel.New(modelToken);

			using var log = logFilePath == null ? null : new StreamWriter(logFilePath);
			var cachingProvider = new PrecompiledCachingProvider(YieldFiles(path, searchOption), log);

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

			var engine = new RazorLightEngineBuilder()
				.UseCachingProvider(cachingProvider)
				.Build();

			var templatePage = cachingProvider.RetrieveTemplate(key).Template.TemplatePageFactory();
			Program.ConsoleOut.WriteLine(engine.RenderTemplateAsync(templatePage, model).GetAwaiter().GetResult());
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
