using System.IO;

using System.Threading;

namespace RazorLight
{
	/// <summary>Exposes the writer, cancellation token, and supplemental values for one render.</summary>
	public interface IPageContext
	{
		/// <summary>
		/// Gets the current writer.
		/// </summary>
		/// <value>The writer.</value>
		TextWriter Writer { get; set; }

		/// <summary>Gets the cancellation token for the current render operation.</summary>
		CancellationToken CancellationToken => CancellationToken.None;


		/// <summary>Gets the dynamic supplemental values supplied by the caller.</summary>
		dynamic ViewBag { get; }
	}
}
