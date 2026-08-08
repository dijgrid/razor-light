using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RazorLight;
using RazorLight.Extensions;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddRazorLight(() => new RazorLightEngineBuilder()
	.UseNoProject()
	.Build());

using var host = builder.Build();
var engine = host.Services.GetRequiredService<IRazorLightEngine>();
string rendered = await engine.CompileRenderStringAsync(
	"worker-probe",
	"Worker @Model",
	"probe");

if (!string.Equals(rendered, "Worker probe", StringComparison.Ordinal))
{
	throw new InvalidOperationException($"Unexpected worker output: {rendered}");
}

Console.WriteLine("RazorLight worker probe passed.");
