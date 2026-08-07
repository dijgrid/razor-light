namespace RazorLight.Compilation
{
	/// <summary>
	/// Controls which metadata references RazorLight discovers automatically for runtime compilation.
	/// </summary>
	public enum MetadataReferenceDiscoveryMode
	{
		/// <summary>
		/// References the operating assembly, RazorLight's runtime dependencies, and explicitly included
		/// assemblies. Other host dependencies must be configured explicitly.
		/// </summary>
		Minimal = 0,

		/// <summary>
		/// References every compile-time library in the operating assembly's dependency context.
		/// This compatibility mode exposes the host's complete compilation dependency graph to templates.
		/// </summary>
		All = 1
	}
}
