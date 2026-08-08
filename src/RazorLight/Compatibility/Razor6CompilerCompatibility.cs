using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using RazorLight.Instrumentation;

namespace RazorLight.Compatibility
{
	/// <summary>
	/// Contains the package-specific integration with the retained public runtime
	/// Razor compiler packages. Keep Razor 6 construction and feature
	/// registration in this boundary until a supported runtime replacement exists.
	/// </summary>
	internal static class Razor6CompilerCompatibility
	{
		private const int SupportedCompilerMajorVersion = 6;

		public static RazorEngine CreateEngine()
		{
			EnsureSupportedCompilerVersion();

			var configuration = RazorConfiguration.Default;
			var razorProjectEngine = RazorProjectEngine.Create(configuration, new NullRazorProjectFileSystem(), builder =>
			{
				Instrumentation.InjectDirective.Register(builder);
				Instrumentation.ModelDirective.Register(builder);
				CompileSourceDirective.Register(builder);

				// In Razor language version 3.0 and later these directives are registered by default.
				if (!RazorLanguageVersion.TryParse("3.0", out var razorLanguageVersion)
					|| configuration.LanguageVersion.CompareTo(razorLanguageVersion) < 0)
				{
					NamespaceDirective.Register(builder);
					FunctionsDirective.Register(builder);
					InheritsDirective.Register(builder);
				}

				SectionDirective.Register(builder);
				builder.Features.Add(new ModelExpressionPass());
				builder.Features.Add(new RazorLightTemplateDocumentClassifierPass());
				builder.Features.Add(new RazorLightAssemblyAttributeInjectionPass());
				builder.AddTargetExtension(new TemplateTargetExtension
				{
					TemplateTypeName = "global::RazorLight.Razor.RazorLightHelperResult",
				});

				OverrideRuntimeNodeWriterTemplateTypeNamePhase.Register(builder);
			});

			return razorProjectEngine.Engine;
		}

		private static void EnsureSupportedCompilerVersion()
		{
			Version? compilerVersion = typeof(RazorEngine).Assembly.GetName().Version;
			if (compilerVersion?.Major != SupportedCompilerMajorVersion)
			{
				throw new InvalidOperationException(
					$"The RazorLight compatibility adapter requires Razor compiler major version " +
					$"{SupportedCompilerMajorVersion}, but version '{compilerVersion}' was loaded.");
			}
		}

		private sealed class NullRazorProjectFileSystem : RazorProjectFileSystem
		{
			public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
			{
				throw new NotImplementedException();
			}

			public override RazorProjectItem GetItem(string path)
			{
				throw new NotImplementedException();
			}

			public override RazorProjectItem GetItem(string path, string fileKind)
			{
				throw new NotImplementedException();
			}
		}
	}
}
