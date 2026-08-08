using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RazorLight.Razor
{
	public sealed class NoRazorProject : RazorLightProject
	{
		public override Task<RazorLightProjectItem> GetItemAsync(string templateKey)
			=> GetItemAsync(templateKey, CancellationToken.None);

		public override Task<RazorLightProjectItem> GetItemAsync(string templateKey, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return Task.FromResult((RazorLightProjectItem)NoRazorProjectItem.Empty);
		}

		public override Task<IEnumerable<RazorLightProjectItem>> GetImportsAsync(string templateKey)
			=> GetImportsAsync(templateKey, CancellationToken.None);

		public override Task<IEnumerable<RazorLightProjectItem>> GetImportsAsync(string templateKey, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return Task.FromResult(Enumerable.Empty<RazorLightProjectItem>());
		}

		public override Task<IEnumerable<string>> GetKnownKeysAsync()
			=> GetKnownKeysAsync(CancellationToken.None);

		public override Task<IEnumerable<string>> GetKnownKeysAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return Task.FromResult(Enumerable.Empty<string>());
		}
	}
}
