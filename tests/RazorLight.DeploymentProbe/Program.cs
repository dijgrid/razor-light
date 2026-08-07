using RazorLight;

var engine = new RazorLightEngineBuilder()
	.UseNoProject()
	.SetOperatingAssembly(typeof(Program).Assembly)
	.Build();

const string template = "Hello @Model.Name! RazorLight is running on @System.Runtime.InteropServices.RuntimeInformation.OSDescription.";
var model = new DeploymentModel("deployment probe");
string rendered = await engine.CompileRenderStringAsync("deployment-probe", template, model);

if (!rendered.StartsWith("Hello deployment probe! RazorLight is running on ", StringComparison.Ordinal))
{
	throw new InvalidOperationException($"Unexpected RazorLight output: {rendered}");
}

Console.WriteLine("RazorLight deployment probe passed.");

public sealed record DeploymentModel(string Name);
