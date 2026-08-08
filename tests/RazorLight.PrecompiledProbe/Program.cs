using RazorLight;
using RazorLight.Caching;

using var cache = new MemoryCachingProvider();
cache.CacheTemplate("greeting", static () => new GreetingPage(), expirationToken: null);
using IRazorLightEngine engine = RazorLightEngineBuilder.CreatePrecompiled(cache);

if (!cache.TryGetTemplate("greeting", out Func<ITemplatePage>? factory))
{
	throw new InvalidOperationException("The precompiled page factory was not registered.");
}

string output = await engine.RenderTemplateAsync(factory(), "world");
if (output != "Hello world")
{
	throw new InvalidOperationException($"Unexpected precompiled output: '{output}'.");
}

string[] forbiddenAssemblies = AppDomain.CurrentDomain.GetAssemblies()
	.Select(assembly => assembly.GetName().Name ?? string.Empty)
	.Where(name =>
		name.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal) ||
		name.Equals("Microsoft.AspNetCore.Razor.Language", StringComparison.Ordinal))
	.ToArray();
if (forbiddenAssemblies.Length != 0)
{
	throw new InvalidOperationException(
		"Precompiled-only execution loaded compiler assemblies: " + string.Join(", ", forbiddenAssemblies));
}

Console.WriteLine("RazorLight precompiled-only probe passed.");

internal sealed class GreetingPage : TemplatePage<string>
{
	public override Task ExecuteAsync()
	{
		WriteLiteral("Hello ");
		Write(Model);
		return Task.CompletedTask;
	}
}
