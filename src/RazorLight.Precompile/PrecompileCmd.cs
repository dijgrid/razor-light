using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RazorLight.Caching;
using System;
using System.Collections.Generic;
using System.IO;

namespace RazorLight.Precompile
{
	public class PrecompileCmd
	{
		private enum StrategyName
		{
			Simple,
			FileHash
		}

		private static readonly Dictionary<StrategyName, IFileSystemCachingStrategy> s_strategyMap = new()
		{
			[StrategyName.Simple] = SimpleFileCachingStrategy.Instance,
			[StrategyName.FileHash] = FileHashCachingStrategy.Instance
		};

		private string m_templateFile;
		private string m_cacheDir;
		private string m_baseDir;
		private StrategyName m_strategyName = StrategyName.FileHash;
		private string m_modelFilePath;
		private string m_jsonQuery;

		public int Run(string[] args)
		{
			var options = CommandLineArguments.Parse(args, new[]
			{
				"-t", "--template", "-c", "--cache", "-b", "--base", "-s", "--strategy",
				"-m", "--model", "-q", "--jsonQuery"
			});
			m_templateFile = options.GetRequiredValue("-t", "--template");
			m_cacheDir = options.GetValue("-c", "--cache");
			m_baseDir = options.GetValue("-b", "--base");
			m_modelFilePath = options.GetValue("-m", "--model");
			m_jsonQuery = options.GetValue("-q", "--jsonQuery");

			var strategy = options.GetValue("-s", "--strategy");
			if (strategy != null && !Enum.TryParse(strategy, true, out m_strategyName))
			{
				throw new RazorLightException("Unsupported strategy " + strategy);
			}

			string templateKey;
			if (m_baseDir == null)
			{
				m_templateFile = Path.GetFullPath(m_templateFile);
				m_baseDir = Path.GetDirectoryName(m_templateFile);
				templateKey = Path.GetFileName(m_templateFile);
			}
			else
			{
				if (!Directory.Exists(m_baseDir))
				{
					throw new RazorLightException($"The razor template base directory {m_baseDir} does not exist.");
				}

				m_baseDir = Path.GetFullPath(m_baseDir);
				if (Path.IsPathRooted(m_templateFile))
				{
					templateKey = Path.GetRelativePath(m_baseDir, m_templateFile);
				}
				else
				{
					templateKey = m_templateFile;
					m_templateFile = Path.GetFullPath(Path.Combine(m_baseDir, m_templateFile));
				}
			}

			if (!File.Exists(m_templateFile))
			{
				throw new RazorLightException($"The razor template file {m_templateFile} does not exist.");
			}

			if (m_cacheDir == null)
			{
				m_cacheDir = Path.GetDirectoryName(m_templateFile);
			}
			else if (!Directory.Exists(m_cacheDir))
			{
				Directory.CreateDirectory(m_cacheDir);
			}

			var provider = new FileSystemCachingProvider(m_baseDir, m_cacheDir, s_strategyMap[m_strategyName]);
			var engine = new RazorLightEngineBuilder()
				.UseFileSystemProject(m_baseDir, "")
				.UseCachingProvider(provider)
				.Build();

			if (m_modelFilePath == null)
			{
				engine.CompileTemplateAsync(templateKey).GetAwaiter().GetResult();
				Program.ConsoleOut.WriteLine(provider.GetAssemblyFilePath(templateKey, m_templateFile));
			}
			else
			{
				var modelToken = JsonConvert.DeserializeObject<JToken>(File.ReadAllText(m_modelFilePath));
				if (m_jsonQuery != null)
				{
					modelToken = modelToken.SelectToken(m_jsonQuery);
				}

				var model = JsonModel.New(modelToken);
				Program.ConsoleOut.WriteLine(engine.CompileRenderAsync(templateKey, model).GetAwaiter().GetResult());
			}

			return 0;
		}
	}
}
