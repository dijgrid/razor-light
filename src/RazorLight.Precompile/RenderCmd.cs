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
		private string m_path;
		private string m_modelFilePath;
		private string m_jsonQuery;
		private SearchOption m_searchOption = SearchOption.TopDirectoryOnly;
		private string m_key;
		private string m_logFilePath;

		public int Run(string[] args)
		{
			var options = CommandLineArguments.Parse(
				args,
				new[] { "-p", "--path", "-m", "--model", "-k", "--key", "-q", "--jsonQuery", "-l", "--log" },
				new[] { "-r", "--recurse" });
			m_path = options.GetRequiredValue("-p", "--path");
			m_modelFilePath = options.GetRequiredValue("-m", "--model");
			m_key = options.GetValue("-k", "--key");
			m_jsonQuery = options.GetValue("-q", "--jsonQuery");
			m_logFilePath = options.GetValue("-l", "--log");
			m_searchOption = options.HasFlag("-r", "--recurse")
				? SearchOption.AllDirectories
				: SearchOption.TopDirectoryOnly;

			var modelToken = JsonConvert.DeserializeObject<JToken>(File.ReadAllText(m_modelFilePath));
			if (m_jsonQuery != null)
			{
				modelToken = modelToken.SelectToken(m_jsonQuery);
			}

			var model = JsonModel.New(modelToken);

			using var log = m_logFilePath == null ? null : new StreamWriter(m_logFilePath);
			var cachingProvider = new PrecompiledCachingProvider(YieldFiles(), log);

			if (m_key == null)
			{
				if (cachingProvider.Map.Count > 1)
				{
					throw new RazorLightException($"Found {cachingProvider.Map.Count} precompiled templates and no --key argument was given.");
				}

				m_key = cachingProvider.Map.First().Key;
			}
			else if (m_key[0] != '/')
			{
				m_key = '/' + m_key;
			}

			var engine = new RazorLightEngineBuilder()
				.UseCachingProvider(cachingProvider)
				.Build();

			var templatePage = cachingProvider.RetrieveTemplate(m_key).Template.TemplatePageFactory();
			Program.ConsoleOut.WriteLine(engine.Handler.RenderTemplateAsync(templatePage, model).GetAwaiter().GetResult());
			return 0;
		}

		private IEnumerable<string> YieldFiles()
		{
			if (m_path.Contains(','))
			{
				return m_path.Split(',').SelectMany(DoYieldFiles);
			}

			return DoYieldFiles(m_path);

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
					return Directory.EnumerateFiles(fileOrFolderPath, "*.dll", m_searchOption);
				}

				throw new RazorLightException($"{fileOrFolderPath} is not found.");
			}
		}
	}
}
