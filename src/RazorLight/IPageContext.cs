using System.IO;

using System.Threading;

namespace RazorLight
{
	public interface IPageContext
	{
		/// <summary>
		/// Gets the current writer.
		/// </summary>
		/// <value>The writer.</value>
		TextWriter Writer { get; set; }

		/// <summary>Gets the cancellation token for the current render operation.</summary>
		CancellationToken CancellationToken => CancellationToken.None;


		dynamic ViewBag { get; }
	}
}
