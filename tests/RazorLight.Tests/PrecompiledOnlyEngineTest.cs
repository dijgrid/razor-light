using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RazorLight.Caching;
using RazorLight.Compilation;
using RazorLight.Extensions;
using RazorLight.Internal;
using RazorLight.Text;
using Xunit;

namespace RazorLight.Tests
{
	public class PrecompiledOnlyEngineTest
	{
		[Fact]
		public async Task Renders_Layout_Include_Encoding_And_Injected_Service_Without_Compiler_Graph()
		{
			using var cache = new MemoryCachingProvider();
			cache.CacheTemplate("main", static () => new MainPage(), null);
			cache.CacheTemplate("include", static () => new IncludePage(), null);
			cache.CacheTemplate("layout", static () => new LayoutPage(), null);
			var services = new ServiceCollection();
			services.AddSingleton(new PrefixService("service:"));
			services.AddRazorLight(() => RazorLightEngineBuilder.CreatePrecompiled(
				cache,
				new RazorLightOptions { OutputEncoder = new BracketEncoder() }));

			using ServiceProvider provider = services.BuildServiceProvider();
			using IRazorLightEngine engine = provider.GetRequiredService<IRazorLightEngine>();
			Assert.True(cache.TryGetTemplate("main", out Func<ITemplatePage>? factory));
			string result = await engine.RenderTemplateAsync(factory(), "value");

			Assert.Equal("layoutservice:[value]body", result);
			var builtIn = Assert.IsType<RazorLightEngine>(engine);
			Assert.IsType<PrecompiledTemplateCompiler>(builtIn.Handler.Compiler);
			Assert.Null(builtIn.Handler.Cache as ICoordinatedCachingProvider);
		}

		[Fact]
		public async Task Miss_And_Runtime_Source_Never_Fall_Back_To_Compilation()
		{
			using var cache = new MemoryCachingProvider();
			using IRazorLightEngine engine = RazorLightEngineBuilder.CreatePrecompiled(cache);

			var missing = await Assert.ThrowsAsync<TemplateNotFoundException>(() =>
				engine.CompileTemplateAsync("missing"));
			Assert.Contains("never falls back", missing.Message);
			var source = await Assert.ThrowsAsync<RazorLightException>(() =>
				engine.CompileRenderStringAsync("runtime", "content", new object()));
			Assert.Contains("not supported in precompiled-only mode", source.Message);
		}

		[Fact]
		public async Task Strong_Model_Contract_Is_Validated_At_Render_Time()
		{
			using var cache = new MemoryCachingProvider();
			cache.CacheTemplate("typed", static () => new IncludePage(), null);
			using IRazorLightEngine engine = RazorLightEngineBuilder.CreatePrecompiled(cache);
			Assert.True(cache.TryGetTemplate("typed", out Func<ITemplatePage>? factory));

			await Assert.ThrowsAnyAsync<Exception>(() => engine.RenderTemplateAsync(factory(), 42));
		}

		[Fact]
		public async Task Embedded_Template_Factory_Can_Move_From_Compilation_To_Precompiled_Execution()
		{
			using var cache = new MemoryCachingProvider();
			using (IRazorLightEngine compilerEngine = new RazorLightEngineBuilder()
				.UseEmbeddedResourcesProject(typeof(Root))
				.UseCachingProvider(cache)
				.Build())
			{
				await compilerEngine.CompileTemplateAsync("Assets.Embedded.Empty.cshtml");
			}

			using IRazorLightEngine runtimeEngine = RazorLightEngineBuilder.CreatePrecompiled(cache);
			string result = await runtimeEngine.CompileRenderAsync<object?>("Assets.Embedded.Empty.cshtml", null);
			Assert.Equal("Empty", result);
		}

		private sealed class PrefixService
		{
			public PrefixService(string value) => Value = value;
			public string Value { get; }
		}

		private sealed class MainPage : TemplatePage<string>
		{
			[RazorInject]
			public PrefixService Prefix { get; set; } = null!;

			public override async Task ExecuteAsync()
			{
				Layout = "layout";
				Write(Prefix.Value);
				await IncludeAsync("include", Model);
				WriteLiteral("body");
			}
		}

		private sealed class IncludePage : TemplatePage<string>
		{
			public override Task ExecuteAsync()
			{
				Write(Model);
				return Task.CompletedTask;
			}
		}

		private sealed class LayoutPage : TemplatePage<string>
		{
			public override Task ExecuteAsync()
			{
				WriteLiteral("layout");
				Write(RenderBody());
				return Task.CompletedTask;
			}
		}

		private sealed class BracketEncoder : IOutputEncoder
		{
			public void Encode(TextWriter writer, string value) => writer.Write($"[{value}]");
		}
	}
}
