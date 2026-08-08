using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Options;
using RazorLight.Generation;
using RazorLight.Internal;
using DependencyContextCompilationOptions = Microsoft.Extensions.DependencyModel.CompilationOptions;

namespace RazorLight.Compilation
{
	public class RoslynCompilationService : ICompilationService
	{
		private readonly IMetadataReferenceManager metadataReferenceManager;
		private readonly bool isDevelopment;
		private readonly bool includeDetailedDiagnostics;
		private readonly List<MetadataReference> metadataReferences = new List<MetadataReference>();
		private readonly IPrecompileCallback? precompileCallback;

		public RoslynCompilationService(IMetadataReferenceManager referenceManager, Assembly operatingAssembly, IPrecompileCallback? precompileCallback = null)
			: this(referenceManager, operatingAssembly, includeDetailedDiagnostics: false, precompileCallback)
		{
		}

		internal RoslynCompilationService(
			IMetadataReferenceManager referenceManager,
			Assembly operatingAssembly,
			bool includeDetailedDiagnostics,
			IPrecompileCallback? precompileCallback = null)
		{
			this.metadataReferenceManager = referenceManager ?? throw new ArgumentNullException(nameof(referenceManager));
			this.OperatingAssembly = operatingAssembly ?? throw new ArgumentNullException(nameof(operatingAssembly));
			this.includeDetailedDiagnostics = includeDetailedDiagnostics;
			this.precompileCallback = precompileCallback;

			isDevelopment = AssemblyDebugModeUtility.IsAssemblyDebugBuild(OperatingAssembly);
			EmitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb);
		}

		public RoslynCompilationService(IMetadataReferenceManager referenceManager, IOptions<RazorLightOptions> options, IPrecompileCallback? precompileCallback = null) :
			this(
				referenceManager,
				(options ?? throw new ArgumentNullException(nameof(options))).Value.OperatingAssembly
					?? throw new InvalidOperationException("RazorLightOptions.OperatingAssembly must be configured."),
				options.Value.EnableDebugMode ?? false,
				precompileCallback)
		{
			
		}

		#region Options

		public virtual Assembly OperatingAssembly { get; }

		public virtual EmitOptions EmitOptions { get; }
		public virtual CSharpCompilationOptions CSharpCompilationOptions
		{
			get
			{
				EnsureOptions();
				return _compilationOptions!;
			}
		}
		public virtual CSharpParseOptions ParseOptions
		{
			get
			{
				EnsureOptions();
				return _parseOptions!;
			}
		}

		#endregion

		private CSharpParseOptions? _parseOptions;
		private CSharpCompilationOptions? _compilationOptions;

		private static readonly object locker = new object();

		private bool _optionsInitialized;
		private void EnsureOptions()
		{
			lock (locker)
			{
				if (!_optionsInitialized)
				{
					var dependencyContextOptions = GetDependencyContextCompilationOptions();
					_parseOptions = GetParseOptions(dependencyContextOptions);
					_compilationOptions = GetCompilationOptions(dependencyContextOptions);

					metadataReferences.AddRange(metadataReferenceManager.Resolve(OperatingAssembly));

					_optionsInitialized = true;
				}
			}
		}


		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Assembly CompileAndEmit(IGeneratedRazorTemplate razorTemplate)
		{
			if (razorTemplate == null)
			{
				throw new ArgumentNullException(nameof(razorTemplate));
			}

			if (!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported)
			{
				throw new PlatformNotSupportedException(
					"RazorLight runtime template compilation is not supported when dynamic code generation is unavailable. " +
					"See " + DeploymentCompatibility.DocumentationUrl + ".");
			}

			string assemblyName = Path.GetRandomFileName();
			var compilation = CreateCompilation(razorTemplate, assemblyName);

			using (var assemblyStream = new MemoryStream())
			using (var pdbStream = new MemoryStream())
			{
				var result = compilation.Emit(
					assemblyStream,
					pdbStream,
					options: EmitOptions);

				if (!result.Success)
				{
					List<Diagnostic> errorsDiagnostics = result.Diagnostics
							.Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error)
							.ToList();

					StringBuilder builder = new StringBuilder();
					builder.AppendLine("Failed to compile generated Razor template:");

					var compilationDiagnostics = new List<TemplateCompilationDiagnostic>();
					
					foreach (Diagnostic diagnostic in errorsDiagnostics)
					{
						FileLinePositionSpan lineSpan = diagnostic.Location.GetMappedLineSpan();
						string errorMessage = includeDetailedDiagnostics
							? diagnostic.GetMessage()
							: $"Compiler diagnostic {diagnostic.Id}. Enable RazorLightOptions.EnableDebugMode for detailed compiler diagnostics.";
						string formattedMessage = $"- ({lineSpan.StartLinePosition.Line}:{lineSpan.StartLinePosition.Character}) {errorMessage}";
						if (!includeDetailedDiagnostics)
						{
							lineSpan = new FileLinePositionSpan(
								string.Empty,
								lineSpan.StartLinePosition,
								lineSpan.EndLinePosition);
						}

						var compilationDiagnostic = new TemplateCompilationDiagnostic(errorMessage, formattedMessage, lineSpan);
						compilationDiagnostics.Add(compilationDiagnostic);

						builder.AppendLine(formattedMessage);
					}

					if (errorsDiagnostics.Any(diagnostic => diagnostic.Id == "CS1977"))
					{
						builder.AppendLine();
						builder.AppendLine(
							"A lambda cannot be bound to a dynamic model member. Declare @model in the template " +
							"or use a CompileRender overload that accepts an explicit model Type. Adding System.Linq alone does not resolve dynamic dispatch.");
					}

					builder.AppendLine("\nSee CompilationErrors for detailed information");

					throw new TemplateCompilationException(builder.ToString(),compilationDiagnostics);
				}

				assemblyStream.Seek(0, SeekOrigin.Begin);
				pdbStream.Seek(0, SeekOrigin.Begin);

				var rawAssembly = assemblyStream.ToArray();
				var rawSymbolStore = pdbStream.ToArray();
				precompileCallback?.Invoke(razorTemplate, rawAssembly, rawSymbolStore);
				var assembly = Assembly.Load(rawAssembly, rawSymbolStore);

				return assembly;
			}
		}

		protected internal virtual DependencyContextCompilationOptions GetDependencyContextCompilationOptions()
		{
			var dependencyContext = DependencyContext.Load(OperatingAssembly);

			if (dependencyContext?.CompilationOptions != null)
			{
				return dependencyContext.CompilationOptions;
			}

			return DependencyContextCompilationOptions.Default;
		}

		private CSharpCompilation CreateCompilation(IGeneratedRazorTemplate razorTemplate, string assemblyName)
		{
			SourceText sourceText = SourceText.From(razorTemplate.GeneratedCode, Encoding.UTF8);
			SyntaxTree templateTree = CreateSyntaxTree(sourceText).WithFilePath(assemblyName);

			var syntaxTrees = new List<SyntaxTree> { templateTree };
			if (razorTemplate is IGeneratedCSharpSourceContainer sourceContainer)
			{
				foreach (CSharpSourceDocument source in sourceContainer.CSharpSources)
				{
					syntaxTrees.Add(CreateSyntaxTree(SourceText.From(source.Content, Encoding.UTF8)).WithFilePath(source.Key));
				}
			}

			CSharpCompilation compilation = CreateCompilation(assemblyName).AddSyntaxTrees(syntaxTrees);

			compilation = ExpressionRewriter.Rewrite(compilation, templateTree);

			//var compilationContext = new RoslynCompilationContext(compilation);
			//_compilationCallback(compilationContext);
			//compilation = compilationContext.Compilation;
			return compilation;
		}

		public CSharpCompilation CreateCompilation(string assemblyName)
		{
			return CSharpCompilation.Create(
				assemblyName,
				options: CSharpCompilationOptions,
				references: metadataReferences);
		}

		public SyntaxTree CreateSyntaxTree(SourceText sourceText)
		{
			return CSharpSyntaxTree.ParseText(sourceText, options: ParseOptions);
		}

		private CSharpCompilationOptions GetCompilationOptions(DependencyContextCompilationOptions dependencyContextOptions)
		{
			var csharpCompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

			// Disable 1702 until roslyn turns this off by default
			csharpCompilationOptions = csharpCompilationOptions.WithSpecificDiagnosticOptions(
				new Dictionary<string, ReportDiagnostic>
				{
					{"CS1701", ReportDiagnostic.Suppress}, // Binding redirects
					{"CS1702", ReportDiagnostic.Suppress},
					{"CS1705", ReportDiagnostic.Suppress}
				});

			if (dependencyContextOptions.AllowUnsafe.HasValue)
			{
				csharpCompilationOptions = csharpCompilationOptions.WithAllowUnsafe(
					dependencyContextOptions.AllowUnsafe.Value);
			}

			OptimizationLevel optimizationLevel;
			if (dependencyContextOptions.Optimize.HasValue)
			{
				optimizationLevel = dependencyContextOptions.Optimize.Value ?
					OptimizationLevel.Release :
					OptimizationLevel.Debug;
			}
			else
			{
				optimizationLevel = isDevelopment ?
					OptimizationLevel.Debug :
					OptimizationLevel.Release;
			}
			csharpCompilationOptions = csharpCompilationOptions.WithOptimizationLevel(optimizationLevel);

			if (dependencyContextOptions.WarningsAsErrors.HasValue)
			{
				var reportDiagnostic = dependencyContextOptions.WarningsAsErrors.Value ?
					ReportDiagnostic.Error :
					ReportDiagnostic.Default;
				csharpCompilationOptions = csharpCompilationOptions.WithGeneralDiagnosticOption(reportDiagnostic);
			}

			return csharpCompilationOptions;
		}

		private CSharpParseOptions GetParseOptions(DependencyContextCompilationOptions dependencyContextOptions)
		{
			var configurationSymbol = isDevelopment ? "DEBUG" : "RELEASE";
			var defines = dependencyContextOptions.Defines.OfType<string>().Concat(new[] { configurationSymbol });

			// RazorLight's maintained runtime baseline is .NET 10, whose supported
			// language version is C# 14. A consuming application's dependency context
			// describes how that application was built; it must not silently downgrade
			// or otherwise select the language used to compile generated templates.
			return new CSharpParseOptions(
				languageVersion: LanguageVersion.CSharp14,
				preprocessorSymbols: defines);
		}
	}
}
