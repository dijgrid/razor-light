using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RazorLight.Razor
{
	public abstract class RazorLightProject
	{
		/// <summary>
		/// Looks up for the template source with a given <paramref name="templateKey"/>
		/// </summary>
		/// <param name="templateKey">Unique template key</param>
		/// <returns></returns>
		public abstract Task<RazorLightProjectItem> GetItemAsync(string templateKey);

		/// <summary>Looks up a template while observing cancellation.</summary>
		/// <remarks>The default implementation cancels only the wait around the legacy override. Override this method to cancel project I/O itself.</remarks>
		public virtual Task<RazorLightProjectItem> GetItemAsync(string templateKey, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return GetItemAsync(templateKey).WaitAsync(cancellationToken);
		}

		/// <summary>
		/// Looks up a C# source item without applying a template extension.
		/// </summary>
		public virtual Task<RazorLightProjectItem> GetSourceItemAsync(string sourceKey)
		{
			return GetItemAsync(sourceKey);
		}

		/// <summary>Looks up a C# source item while observing cancellation.</summary>
		/// <remarks>The default implementation cancels only the wait around the legacy override. Override this method to cancel project I/O itself.</remarks>
		public virtual Task<RazorLightProjectItem> GetSourceItemAsync(string sourceKey, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return GetSourceItemAsync(sourceKey).WaitAsync(cancellationToken);
		}

		/// <summary>
		/// Looks up for the ViewImports content for the given template
		/// </summary>
		/// <param name="templateKey"></param>
		/// <returns></returns>
		public abstract Task<IEnumerable<RazorLightProjectItem>> GetImportsAsync(string templateKey);

		/// <summary>Looks up template imports while observing cancellation.</summary>
		/// <remarks>The default implementation cancels only the wait around the legacy override. Override this method to cancel project I/O itself.</remarks>
		public virtual Task<IEnumerable<RazorLightProjectItem>> GetImportsAsync(string templateKey, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return GetImportsAsync(templateKey).WaitAsync(cancellationToken);
		}

		/// <summary>
		/// Looks up all template keys known by the project
		/// </summary>
		/// <returns></returns>
		public virtual Task<IEnumerable<string>> GetKnownKeysAsync()
		{
			return Task.FromResult(Enumerable.Empty<string>());
		}

		/// <summary>Looks up known template keys while observing cancellation.</summary>
		/// <remarks>The default implementation cancels only the wait around the legacy override. Override this method to cancel project I/O itself.</remarks>
		public virtual Task<IEnumerable<string>> GetKnownKeysAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return GetKnownKeysAsync().WaitAsync(cancellationToken);
		}

		public virtual string NormalizeKey(string templateKey) => templateKey;
	}
}
