using System;
using System.IO;
using System.Threading.Tasks;
using RazorLight.Text;

namespace RazorLight.Tests.Utils
{
	/// <summary>
	/// Represents a deferred write operation in a <see cref="TemplatePage"/>.
	/// </summary>
	public class HelperResult : ITemplateContent
	{
		private readonly Func<TextWriter, Task> _asyncAction;

		/// <summary>
		/// Creates a new instance of <see cref="HelperResult"/>.
		/// </summary>
		/// <param name="asyncAction">The asynchronous delegate to invoke when
		/// <see cref="WriteTo(TextWriter)"/> is called.</param>
		/// <remarks>Calls to <see cref="WriteTo(TextWriter)"/> result in a blocking invocation of
		/// <paramref name="asyncAction"/>.</remarks>
		public HelperResult(Func<TextWriter, Task> asyncAction)
		{
			if (asyncAction == null)
			{
				throw new ArgumentNullException(nameof(asyncAction));
			}

			_asyncAction = asyncAction;
		}

		/// <summary>
		/// Gets the asynchronous delegate to invoke when <see cref="WriteTo(TextWriter)"/> is called.
		/// </summary>
		public Func<TextWriter, Task> WriteAction => _asyncAction;

		/// <summary>
		/// Method invoked to produce content from the <see cref="HelperResult"/>.
		/// </summary>
		/// <param name="writer">The <see cref="TextWriter"/> instance to write to.</param>
		public virtual void WriteTo(TextWriter writer)
		{
			if (writer == null)
			{
				throw new ArgumentNullException(nameof(writer));
			}

			_asyncAction(writer).GetAwaiter().GetResult();
		}
	}
}
