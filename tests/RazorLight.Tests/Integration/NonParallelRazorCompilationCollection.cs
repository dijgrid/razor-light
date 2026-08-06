using Xunit;

namespace RazorLight.Tests.Integration
{
	[CollectionDefinition(Name, DisableParallelization = true)]
	public sealed class NonParallelRazorCompilationCollection
	{
		public const string Name = "Non-parallel Razor compilation";
	}
}
