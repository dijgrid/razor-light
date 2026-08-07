namespace RazorLight
{
	internal static class DeploymentCompatibility
	{
		internal const string DocumentationUrl =
			"https://github.com/dijgrid/razor-light/blob/master/docs/deployment.md";

		internal const string RequiresDynamicCodeMessage =
			"Runtime Razor compilation requires dynamic code generation and is not supported by Native AOT.";

		internal const string RequiresUnreferencedCodeMessage =
			"Runtime Razor compilation discovers and loads assemblies dynamically and is not supported in trimmed applications.";
	}
}
