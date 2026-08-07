using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using RazorLight.Compilation;
using Xunit;

namespace RazorLight.Tests.Compatibility
{
	public class DeploymentCompatibilityTest
	{
		[Theory]
		[InlineData(typeof(IRazorLightEngine))]
		[InlineData(typeof(RazorLightEngine))]
		[InlineData(typeof(IEngineHandler))]
		[InlineData(typeof(EngineHandler))]
		[InlineData(typeof(IRazorTemplateCompiler))]
		[InlineData(typeof(RazorTemplateCompiler))]
		[InlineData(typeof(ICompilationService))]
		[InlineData(typeof(RoslynCompilationService))]
		public void RuntimeCompilationEntryPointsDeclareDeploymentRequirements(Type engineType)
		{
			MethodInfo[] compilationMethods = engineType
				.GetMethods(BindingFlags.Instance | BindingFlags.Public)
				.Where(method => method.Name.StartsWith("Compile", StringComparison.Ordinal))
				.ToArray();

			Assert.NotEmpty(compilationMethods);
			Assert.All(compilationMethods, method =>
			{
				var dynamicCode = method.GetCustomAttribute<RequiresDynamicCodeAttribute>();
				var unreferencedCode = method.GetCustomAttribute<RequiresUnreferencedCodeAttribute>();

				Assert.NotNull(dynamicCode);
				Assert.NotNull(unreferencedCode);
				Assert.Contains("Native AOT", dynamicCode!.Message, StringComparison.Ordinal);
				Assert.Contains("trimmed", unreferencedCode!.Message, StringComparison.Ordinal);
				Assert.EndsWith("docs/deployment.md", dynamicCode.Url, StringComparison.Ordinal);
				Assert.Equal(dynamicCode.Url, unreferencedCode.Url);
			});
		}
	}
}
