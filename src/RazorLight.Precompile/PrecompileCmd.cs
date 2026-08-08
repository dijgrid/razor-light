using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RazorLight.Caching;
using RazorLight.Razor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

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

		public int Run(string[] args) => Run(args, CancellationToken.None);

		public int Run(string[] args, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var options = CommandLineArguments.Parse(args, new[]
			{
				"-t", "--template", "-c", "--cache", "-b", "--base", "-s", "--strategy",
				"-m", "--model", "-q", "--jsonQuery"
			});
			var templateFile = options.GetRequiredValue("-t", "--template");
			var cacheDir = options.GetValue("-c", "--cache");
			var baseDir = options.GetValue("-b", "--base");
			var modelFilePath = options.GetValue("-m", "--model");
			var jsonQuery = options.GetValue("-q", "--jsonQuery");
			var strategyName = StrategyName.FileHash;

			var strategy = options.GetValue("-s", "--strategy");
			if (strategy != null && !Enum.TryParse(strategy, true, out strategyName))
			{
				throw new RazorLightException("Unsupported strategy " + strategy);
			}

			string templateKey;
			if (baseDir == null)
			{
				templateFile = Path.GetFullPath(templateFile);
				baseDir = Path.GetDirectoryName(templateFile)
					?? throw new RazorLightException($"Could not determine the base directory for {templateFile}.");
				templateKey = Path.GetFileName(templateFile);
			}
			else
			{
				if (!Directory.Exists(baseDir))
				{
					throw new RazorLightException($"The razor template base directory {baseDir} does not exist.");
				}

				baseDir = FileSystemRazorProjectHelper.NormalizeRoot(baseDir);
				if (Path.IsPathRooted(templateFile))
				{
					templateKey = Path.GetRelativePath(baseDir, Path.GetFullPath(templateFile));
				}
				else
				{
					templateKey = templateFile;
				}

				templateFile = FileSystemRazorProjectHelper.ResolveContainedPath(
					baseDir,
					templateKey,
					"template path");
			}

			if (!File.Exists(templateFile))
			{
				throw new RazorLightException($"The razor template file {templateFile} does not exist.");
			}

			if (cacheDir == null)
			{
				cacheDir = Path.GetDirectoryName(templateFile)
					?? throw new RazorLightException($"Could not determine the cache directory for {templateFile}.");
			}
			else if (!Directory.Exists(cacheDir))
			{
				Directory.CreateDirectory(cacheDir);
			}

			using var provider = new FileSystemCachingProvider(baseDir, cacheDir, s_strategyMap[strategyName]);
			using var engine = new RazorLightEngineBuilder()
				.UseFileSystemProject(baseDir, "")
				.UseCachingProvider(provider)
				.Build();

			if (modelFilePath == null)
			{
				engine.CompileTemplateAsync(templateKey, cancellationToken).GetAwaiter().GetResult();
				Program.ConsoleOut.WriteLine(provider.GetAssemblyFilePath(templateKey, templateFile));
			}
			else
			{
				var modelToken = JsonConvert.DeserializeObject<JToken>(File.ReadAllText(modelFilePath));
				if (jsonQuery != null)
				{
					modelToken = modelToken?.SelectToken(jsonQuery);
				}

				var model = JsonModel.New(modelToken);
				Program.ConsoleOut.WriteLine(engine.CompileRenderAsync(templateKey, model, cancellationToken).GetAwaiter().GetResult());
			}

			return 0;
		}
	}
}
