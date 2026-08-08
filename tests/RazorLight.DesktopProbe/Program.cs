using RazorLight;

var engine = new RazorLightEngineBuilder()
	.UseNoProject()
	.Build();
string rendered = await engine.CompileRenderStringAsync(
	"desktop-probe",
	"Desktop @Model",
	"probe");

if (!string.Equals(rendered, "Desktop probe", StringComparison.Ordinal))
{
	throw new InvalidOperationException($"Unexpected desktop output: {rendered}");
}

Console.WriteLine("RazorLight desktop probe passed.");
