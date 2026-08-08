using System;
using System.Linq;
using RazorLight.Caching;
using RazorLight.Compilation;
using RazorLight.Generation;
using RazorLight.Razor;
using RazorLight.Text;
using Xunit;

namespace RazorLight.Tests.Compatibility
{
	public class PublicApiTierTest
	{
		[Fact]
		public void Generated_Template_ABI_Types_Are_Public()
		{
			Type[] generatedAbi =
			{
				typeof(ITemplatePage),
				typeof(TemplatePageBase),
				typeof(TemplatePage),
				typeof(TemplatePage<>),
				typeof(IPageContext),
				typeof(PageContext),
				typeof(ModelTypeInfo),
				typeof(RenderAsyncDelegate),
				typeof(RazorInjectAttribute),
				typeof(RazorLightHelperResult),
				typeof(RazorLightTemplateAttribute),
				typeof(ITemplateContent),
				typeof(IRawString),
				typeof(RawString),
				typeof(TemplateContent),
			};

			Assert.All(generatedAbi, type => Assert.True(type.IsPublic, type.FullName));
		}

		[Fact]
		public void Implementation_Namespaces_Are_Not_Exported()
		{
			Type[] exportedTypes = typeof(IRazorLightEngine).Assembly.GetExportedTypes();

			Assert.DoesNotContain(exportedTypes, type =>
				type.Namespace == "RazorLight.Internal" ||
				type.Namespace == "RazorLight.Internal.Buffering" ||
				type.Namespace == "RazorLight.Instrumentation");

			Assert.Equal(
				new[]
				{
					typeof(MetadataReferenceDiscoveryMode),
					typeof(TemplateCompilationDiagnostic),
					typeof(TemplateCompilationException),
				},
				exportedTypes.Where(type => type.Namespace == "RazorLight.Compilation").OrderBy(type => type.Name));
			Assert.Equal(
				new[] { typeof(TemplateGenerationException) },
				exportedTypes.Where(type => type.Namespace == "RazorLight.Generation"));
		}

		[Fact]
		public void Supported_Extension_Contracts_Are_Public()
		{
			Type[] extensionContracts =
			{
				typeof(RazorLightProject),
				typeof(RazorLightProjectItem),
				typeof(ICachingProvider),
				typeof(IFileSystemCachingStrategy),
				typeof(IOutputEncoder),
			};

			Assert.All(extensionContracts, type => Assert.True(type.IsPublic, type.FullName));
		}
	}
}
