using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Options;
using RazorLight.Compilation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace RazorLight.Tests.Compilation
{
	public class DefaultMetadataReferenceManagerTest
	{
		[Fact]
		public void Throws_OnEmptyManager_InConstructor()
		{
			Assert.Throws<ArgumentNullException>(() => { _ = new DefaultMetadataReferenceManager((HashSet<MetadataReference>)null!, (HashSet<string>)null!); });
		}

		[Fact]
		public void Ensure_AdditionalMetadata_IsApplied()
		{
			var metadata = new HashSet<MetadataReference>();
			var manager = new DefaultMetadataReferenceManager(metadata);

			Assert.NotNull(manager.AdditionalMetadataReferences);
			Assert.Equal(metadata, manager.AdditionalMetadataReferences);
		}

		[Fact]
		public void Resolve_SkipsAssembliesWithoutUsableLocations()
		{
			var runtimeReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
			var options = Options.Create(new RazorLightOptions
			{
				AdditionalMetadataReferences = new HashSet<MetadataReference> { runtimeReference }
			});
			var manager = new DefaultMetadataReferenceManager(options, new EmptyAssemblyPathFormatter());

			IReadOnlyList<MetadataReference> references = manager.Resolve(
				typeof(DefaultMetadataReferenceManagerTest).Assembly,
				dependencyContext: null);

			Assert.Contains(runtimeReference, references);
		}

		[Fact]
		public void Resolve_ReportsActionableErrorWhenNoUsableReferencesExist()
		{
			var options = Options.Create(new RazorLightOptions());
			var manager = new DefaultMetadataReferenceManager(options, new EmptyAssemblyPathFormatter());

			var exception = Assert.Throws<RazorLightException>(() => manager.Resolve(
				typeof(DefaultMetadataReferenceManagerTest).Assembly,
				dependencyContext: null));

			Assert.Contains("PreserveCompilationContext", exception.Message, StringComparison.Ordinal);
			Assert.Contains("AddMetadataReferences", exception.Message, StringComparison.Ordinal);
		}

		[Fact]
		public void DefaultPathFormatter_ReturnsEmptyPathForDynamicAssembly()
		{
			Assembly dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(
				new AssemblyName("RazorLight.Dynamic.Metadata.Test"),
				AssemblyBuilderAccess.Run);

			string path = new DefaultAssemblyPathFormatter().GetAssemblyPath(dynamicAssembly);

			Assert.Equal(string.Empty, path);
		}

		[Fact]
		public void Resolve_MinimalDiscovery_IncludesProjectAssemblies_ButNotUnrelatedPackages()
		{
			var manager = new DefaultMetadataReferenceManager();

			IReadOnlyList<MetadataReference> references = manager.Resolve(typeof(DefaultMetadataReferenceManagerTest).Assembly);
			var names = GetAssemblyNames(references);

			Assert.Contains("RazorLight.Tests", names);
			Assert.DoesNotContain("Moq", names);
		}

		[Fact]
		public void Resolve_IncludeAssemblies_AllowsAnExactDependencyContextAssembly()
		{
			var options = Options.Create(new RazorLightOptions
			{
				IncludedAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Moq" }
			});
			var manager = new DefaultMetadataReferenceManager(options, new DefaultAssemblyPathFormatter());

			var names = GetAssemblyNames(manager.Resolve(typeof(DefaultMetadataReferenceManagerTest).Assembly));

			Assert.Contains("Moq", names);
		}

		[Fact]
		public void Resolve_ExcludeAssemblies_UsesExactNames()
		{
			var options = Options.Create(new RazorLightOptions
			{
				ExcludedAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "M" },
				MetadataReferenceDiscovery = MetadataReferenceDiscoveryMode.All
			});
			var manager = new DefaultMetadataReferenceManager(options, new DefaultAssemblyPathFormatter());

			var names = GetAssemblyNames(manager.Resolve(typeof(DefaultMetadataReferenceManagerTest).Assembly));

			Assert.Contains("Moq", names);
		}

		[Fact]
		public void Resolve_AllDiscovery_CanBeSelectedExplicitly()
		{
			var options = Options.Create(new RazorLightOptions
			{
				MetadataReferenceDiscovery = MetadataReferenceDiscoveryMode.All
			});
			var manager = new DefaultMetadataReferenceManager(options, new DefaultAssemblyPathFormatter());

			var names = GetAssemblyNames(manager.Resolve(typeof(DefaultMetadataReferenceManagerTest).Assembly));

			Assert.Contains("Moq", names);
		}

		private static HashSet<string> GetAssemblyNames(IEnumerable<MetadataReference> references) =>
			new HashSet<string>(
				references
					.Select(reference => reference.Display)
					.Where(display => !string.IsNullOrEmpty(display))
					.Select(display => Path.GetFileNameWithoutExtension(display!)),
				StringComparer.OrdinalIgnoreCase);

		private sealed class EmptyAssemblyPathFormatter : IAssemblyPathFormatter
		{
			public string GetAssemblyPath(Assembly assembly) => string.Empty;
		}
	}
}
