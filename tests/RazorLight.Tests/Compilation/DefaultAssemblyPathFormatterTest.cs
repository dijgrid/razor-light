using RazorLight.Compilation;
using System;
using Xunit;
using Xunit.Abstractions;

namespace RazorLight.Tests.Compilation
{
	public class DefaultAssemblyPathFormatterTest
	{
		private readonly ITestOutputHelper _testOutputHelper;

		public DefaultAssemblyPathFormatterTest(ITestOutputHelper testOutputHelper)
		{
			_testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
		}

		[Fact]
		public void Ensure_GetAssemblyPath_Works()
		{
			var assembly = typeof(DefaultAssemblyPathFormatterTest).Assembly;
			_testOutputHelper.WriteLine(assembly.Location);
			var directory = new DefaultAssemblyPathFormatter().GetAssemblyPath(assembly);
			Assert.NotNull(directory);
		}

	}
}
