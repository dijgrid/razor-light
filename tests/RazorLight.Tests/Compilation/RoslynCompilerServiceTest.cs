using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using RazorLight.Compilation;
using RazorLight.Generation;
using RazorLight.Internal;
using RazorLight.Razor;
using Xunit;
using Xunit.Abstractions;
using DependencyContextCompilationOptions = Microsoft.Extensions.DependencyModel.CompilationOptions;

namespace RazorLight.Tests.Compilation
{
	public class RoslynCompilerServiceTest
	{
		private readonly ITestOutputHelper _testOutputHelper;

		public RoslynCompilerServiceTest(ITestOutputHelper testOutputHelper)
		{
			_testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
		}

		[Fact]
		public void Constructor_UsesPortablePdbs()
		{
			var compiler = new RoslynCompilationService(
				new DefaultMetadataReferenceManager(),
				Assembly.GetEntryAssembly()!);

			Assert.Equal(DebugInformationFormat.PortablePdb, compiler.EmitOptions.DebugInformationFormat);
		}

		[Fact]
		public async Task Option_Initialization_Does_Not_Serialize_Unrelated_Compilers()
		{
			using var firstEntered = new ManualResetEventSlim();
			using var releaseFirst = new ManualResetEventSlim();
			var first = new RoslynCompilationService(
				new BlockingReferenceManager(firstEntered, releaseFirst),
				Assembly.GetExecutingAssembly());
			var second = new RoslynCompilationService(
				new DefaultMetadataReferenceManager(),
				Assembly.GetExecutingAssembly());

			Task firstInitialization = Task.Run(() => _ = first.ParseOptions);
			Assert.True(firstEntered.Wait(TimeSpan.FromSeconds(5)));
			Task secondInitialization = Task.Run(() => _ = second.ParseOptions);
			try
			{
				Assert.Same(secondInitialization, await Task.WhenAny(secondInitialization, Task.Delay(TimeSpan.FromSeconds(5))));
			}
			finally
			{
				releaseFirst.Set();
			}

			await Task.WhenAll(firstInitialization, secondInitialization);
		}

		[Fact]
		public void Constructor_SetsCompilationOptionsFromDependencyContext()
		{
			var compiler = new RoslynCompilationService(new DefaultMetadataReferenceManager(),
				Assembly.GetEntryAssembly()!);

			// Act & Assert
			var parseOptions = compiler.ParseOptions;
			Assert.Contains("SOME_TEST_DEFINE", parseOptions.PreprocessorSymbolNames);
		}

		[Fact]
		public void EnsureOptions_ConfiguresCompilationOptions()
		{
			// Arrange
			var compiler = new RoslynCompilationService(new DefaultMetadataReferenceManager(), Assembly.GetEntryAssembly()!);

			// Act & Assert
			var compilationOptions = compiler.CSharpCompilationOptions;
			Assert.True(compilationOptions.AllowUnsafe);
			Assert.Equal(ReportDiagnostic.Default, compilationOptions.GeneralDiagnosticOption);
			Assert.Equal(OptimizationLevel.Debug, compilationOptions.OptimizationLevel);
			Assert.Collection(compilationOptions.SpecificDiagnosticOptions.OrderBy(d => d.Key),
				item =>
				{
					Assert.Equal("CS1701", item.Key);
					Assert.Equal(ReportDiagnostic.Suppress, item.Value);
				},
				item =>
				{
					Assert.Equal("CS1702", item.Key);
					Assert.Equal(ReportDiagnostic.Suppress, item.Value);
				},
				item =>
				{
					Assert.Equal("CS1705", item.Key);
					Assert.Equal(ReportDiagnostic.Suppress, item.Value);
				});
		}

		[Fact]
		public void Constructor_Uses_Maintained_CSharp_Language_Version()
		{
			// Arrange
			var dependencyContextOptions = new DependencyContextCompilationOptions(
				new[] { "MyDefine" },
				languageVersion: "7.1",
				platform: null,
				allowUnsafe: true,
				warningsAsErrors: null,
				optimize: null,
				keyFile: null,
				delaySign: null,
				publicSign: null,
				debugType: null,
				emitEntryPoint: null,
				generateXmlDocumentation: null);

			var compiler = new TestCSharpCompiler(new DefaultMetadataReferenceManager(), dependencyContextOptions);

			// Act & Assert
			var compilationOptions = compiler.ParseOptions;
			Assert.Equal(LanguageVersion.CSharp14, compilationOptions.LanguageVersion);
		}

		[Fact]
		public void Constructor_ConfiguresAllowUnsafe()
		{
			// Arrange
			var dependencyContextOptions = new DependencyContextCompilationOptions(
				new[] { "MyDefine" },
				languageVersion: null,
				platform: null,
				allowUnsafe: true,
				warningsAsErrors: null,
				optimize: null,
				keyFile: null,
				delaySign: null,
				publicSign: null,
				debugType: null,
				emitEntryPoint: null,
				generateXmlDocumentation: null);

			var compiler = new TestCSharpCompiler(
				new DefaultMetadataReferenceManager(), dependencyContextOptions);

			// Act & Assert
			var compilationOptions = compiler.CSharpCompilationOptions;
			Assert.True(compilationOptions.AllowUnsafe);
		}

		[Fact]
		public void Constructor_SetsDiagnosticOption()
		{
			// Arrange
			var dependencyContextOptions = new DependencyContextCompilationOptions(
				new[] { "MyDefine" },
				languageVersion: null,
				platform: null,
				allowUnsafe: null,
				warningsAsErrors: true,
				optimize: null,
				keyFile: null,
				delaySign: null,
				publicSign: null,
				debugType: null,
				emitEntryPoint: null,
				generateXmlDocumentation: null);

			var compiler = new TestCSharpCompiler(new DefaultMetadataReferenceManager(), dependencyContextOptions);

			// Act & Assert
			var compilationOptions = compiler.CSharpCompilationOptions;
			Assert.Equal(ReportDiagnostic.Error, compilationOptions.GeneralDiagnosticOption);
		}

		[Fact]
		public void Constructor_SetsOptimizationLevel()
		{
			// Arrange
			var dependencyContextOptions = new DependencyContextCompilationOptions(
				new[] { "MyDefine" },
				languageVersion: null,
				platform: null,
				allowUnsafe: null,
				warningsAsErrors: null,
				optimize: true,
				keyFile: null,
				delaySign: null,
				publicSign: null,
				debugType: null,
				emitEntryPoint: null,
				generateXmlDocumentation: null);

			var compiler = new TestCSharpCompiler(new DefaultMetadataReferenceManager(), dependencyContextOptions);

			// Act & Assert
			var compilationOptions = compiler.CSharpCompilationOptions;
			Assert.Equal(OptimizationLevel.Release, compilationOptions.OptimizationLevel);
		}

		[Fact]
		public void Constructor_SetsDefines()
		{
			// Arrange
			var dependencyContextOptions = new DependencyContextCompilationOptions(
				new[] { "MyDefine" },
				languageVersion: null,
				platform: null,
				allowUnsafe: null,
				warningsAsErrors: null,
				optimize: true,
				keyFile: null,
				delaySign: null,
				publicSign: null,
				debugType: "none",
				emitEntryPoint: null,
				generateXmlDocumentation: null);

			var compiler = new TestCSharpCompiler(new DefaultMetadataReferenceManager(), dependencyContextOptions);

			// Act & Assert
			var parseOptions = compiler.ParseOptions;

			var expected = new[]
			{
				"MyDefine",
				"RELEASE"
			};

			_testOutputHelper.WriteLine($"{AssemblyDebugModeUtility.IsAssemblyDebugBuild(typeof(Root).Assembly)}");
			Assert.Equal(expected, parseOptions.PreprocessorSymbolNames);
		}

		[Fact]
		public void Compile_UsesApplicationsCompilationSettings_ForParsingAndCompilation()
		{
			// Arrange
			var content = "public class Test {}";
			var define = "MY_CUSTOM_DEFINE";
			var dependencyContextOptions = new DependencyContextCompilationOptions(
				new[] { define },
				languageVersion: null,
				platform: null,
				allowUnsafe: null,
				warningsAsErrors: null,
				optimize: true,
				keyFile: null,
				delaySign: null,
				publicSign: null,
				debugType: null,
				emitEntryPoint: null,
				generateXmlDocumentation: null);
			var compiler = new TestCSharpCompiler(
				new DefaultMetadataReferenceManager(), dependencyContextOptions);
			// Act
			var syntaxTree = compiler.CreateSyntaxTree(SourceText.From(content));
			// Assert
			Assert.Contains(define, syntaxTree.Options.PreprocessorSymbolNames);
		}

		[Fact]
		public void Throw_With_CompilationErrors_On_Failed_BuildAsync()
		{
			var compiler = new RoslynCompilationService(new DefaultMetadataReferenceManager(), Assembly.GetEntryAssembly()!);

			var template = new TestGeneratedRazorTemplate("key", "public class Test { error }");

			var ex = Assert.Throws<TemplateCompilationException>(() => compiler.CompileAndEmit(template));
			Assert.NotEmpty(ex.CompilationErrors);
			Assert.NotEmpty(ex.CompilationDiagnostics);
			Assert.Single(ex.CompilationDiagnostics);
			Assert.Single(ex.CompilationErrors);
		}

		[Fact]
		public void CompilationDiagnostics_IncludeCompilerMessageButRedactMappedPathByDefault()
		{
			const string secret = "DO_NOT_LOG_TEMPLATE_SECRET";
			const string privatePath = "C:/private/templates/customer.cshtml";
			var compiler = new RoslynCompilationService(
				new DefaultMetadataReferenceManager(),
				Assembly.GetEntryAssembly()!);
			var template = new TestGeneratedRazorTemplate(
				"private-template",
				$"#line 1 \"{privatePath}\"\npublic class Test {{ void M() {{ {secret}; }} }}");

			var exception = Assert.Throws<TemplateCompilationException>(() => compiler.CompileAndEmit(template));

			Assert.Contains(secret, exception.Message, StringComparison.Ordinal);
			Assert.DoesNotContain(privatePath, exception.Message, StringComparison.Ordinal);
			Assert.Contains(exception.CompilationDiagnostics, diagnostic =>
				diagnostic.ErrorMessage.Contains(secret, StringComparison.Ordinal));
			Assert.All(exception.CompilationDiagnostics, diagnostic =>
			{
				Assert.Equal(string.Empty, diagnostic.LineSpan?.Path);
			});
		}

		[Fact]
		public void CompilationDiagnostics_CanRedactCompilerMessageIndependentlyOfMappedPath()
		{
			const string secret = "DO_NOT_LOG_TEMPLATE_SECRET";
			const string privatePath = "C:/private/templates/customer.cshtml";
			var compiler = new RoslynCompilationService(
				new DefaultMetadataReferenceManager(),
				Assembly.GetEntryAssembly()!,
				includeDetailedDiagnostics: false,
				redactCompilerDiagnosticMessages: true);
			var template = new TestGeneratedRazorTemplate(
				"private-template",
				$"#line 1 \"{privatePath}\"\npublic class Test {{ void M() {{ {secret}; }} }}");

			var exception = Assert.Throws<TemplateCompilationException>(() => compiler.CompileAndEmit(template));

			Assert.Contains(exception.CompilationDiagnostics, diagnostic =>
				diagnostic.ErrorMessage.Contains("CS0103", StringComparison.Ordinal));
			Assert.All(exception.CompilationDiagnostics, diagnostic =>
			{
				Assert.DoesNotContain(secret, diagnostic.ErrorMessage, StringComparison.Ordinal);
				Assert.Equal(string.Empty, diagnostic.LineSpan?.Path);
			});
		}

		[Fact]
		public void CompilationDiagnostics_IncludeDetailsOnlyWhenEnabled()
		{
			const string secret = "DO_NOT_LOG_TEMPLATE_SECRET";
			const string privatePath = "C:/private/templates/customer.cshtml";
			var compiler = new RoslynCompilationService(
				new DefaultMetadataReferenceManager(),
				Assembly.GetEntryAssembly()!,
				includeDetailedDiagnostics: true);
			var template = new TestGeneratedRazorTemplate(
				"private-template",
				$"#line 1 \"{privatePath}\"\npublic class Test {{ void M() {{ {secret}; }} }}");

			var exception = Assert.Throws<TemplateCompilationException>(() => compiler.CompileAndEmit(template));

			var diagnostic = Assert.Single(
				exception.CompilationDiagnostics,
				item => item.ErrorMessage.Contains(secret, StringComparison.Ordinal));
			Assert.Contains(secret, diagnostic.ErrorMessage, StringComparison.Ordinal);
			Assert.Equal(privatePath, diagnostic.LineSpan?.Path);
		}

		[Fact]
		public void CompilationDiagnostics_MessageRedaction_RemainsEnabledInDebugMode()
		{
			const string secret = "DO_NOT_LOG_TEMPLATE_SECRET";
			const string privatePath = "C:/private/templates/customer.cshtml";
			var compiler = new RoslynCompilationService(
				new DefaultMetadataReferenceManager(),
				Assembly.GetEntryAssembly()!,
				includeDetailedDiagnostics: true,
				redactCompilerDiagnosticMessages: true);
			var template = new TestGeneratedRazorTemplate(
				"private-template",
				$"#line 1 \"{privatePath}\"\npublic class Test {{ void M() {{ {secret}; }} }}");

			var exception = Assert.Throws<TemplateCompilationException>(() => compiler.CompileAndEmit(template));

			var diagnostic = Assert.Single(
				exception.CompilationDiagnostics,
				item => item.ErrorMessage.Contains("CS0103", StringComparison.Ordinal));
			Assert.DoesNotContain(secret, diagnostic.ErrorMessage, StringComparison.Ordinal);
			Assert.Equal(privatePath, diagnostic.LineSpan?.Path);
		}

		[Fact]
		public void Throw_OnNullRazorTemplate_OnCompile()
		{
			var compiler = new RoslynCompilationService(new DefaultMetadataReferenceManager(), Assembly.GetEntryAssembly()!);

			Func<Assembly> action = () => compiler.CompileAndEmit(null!);

			Assert.Throws<ArgumentNullException>(action);
		}

		private class TestGeneratedRazorTemplate : IGeneratedRazorTemplate
		{
			private string generatedCode;
			private string templateKey;

			public TestGeneratedRazorTemplate(string key, string generatedCode)
			{
				this.generatedCode = generatedCode;
				this.templateKey = key;
			}

			public string TemplateKey => templateKey;
			public string GeneratedCode => generatedCode;
			public RazorLightProjectItem ProjectItem
			{
				get
				{
					return new TextSourceRazorProjectItem(TemplateKey, "");
				}
				set
				{

				}
			}
		}


		private class TestCSharpCompiler : RoslynCompilationService
		{
			private readonly DependencyContextCompilationOptions _options;

			public TestCSharpCompiler(IMetadataReferenceManager referenceManager, DependencyContextCompilationOptions options, Assembly? assembly = null)
				: base(referenceManager, assembly ?? Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly())
			{
				_options = options;
			}

			protected internal override DependencyContextCompilationOptions GetDependencyContextCompilationOptions()
				=> _options;
		}

		private sealed class BlockingReferenceManager : IMetadataReferenceManager
		{
			private readonly ManualResetEventSlim _entered;
			private readonly ManualResetEventSlim _release;

			public BlockingReferenceManager(ManualResetEventSlim entered, ManualResetEventSlim release)
			{
				_entered = entered;
				_release = release;
			}

			public HashSet<MetadataReference> AdditionalMetadataReferences { get; } = new();

			public IReadOnlyList<MetadataReference> Resolve(Assembly assembly)
			{
				_entered.Set();
				_release.Wait(TimeSpan.FromSeconds(10));
				return Array.Empty<MetadataReference>();
			}
		}
	}
}
