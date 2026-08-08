using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RazorLight.Razor;

namespace RazorLight.Tests.Razor
{
	public class TestRazorProject : RazorLightProject
	{
		public required RazorLightProjectItem Value { get; set; }

		public override Task<IEnumerable<RazorLightProjectItem>> GetImportsAsync(string templateKey)
		{
			return Task.FromResult(Enumerable.Empty<RazorLightProjectItem>());
		}

		public override Task<RazorLightProjectItem> GetItemAsync(string templateKey)
		{
			return Task.FromResult(Value);
		}
	}
}
