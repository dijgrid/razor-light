using System;
using System.IO;
using System.Threading.Tasks;
using RazorLight.Text;

namespace RazorLight.Razor
{
	/// <inheritdoc />
	public class RazorLightHelperResult : ITemplateContent
	{
		private readonly Func<TextWriter, Task> _writeAction;

		/// <inheritdoc />
		public RazorLightHelperResult(Func<TextWriter, Task> asyncAction)
		{
			_writeAction = asyncAction ?? throw new ArgumentNullException(nameof(asyncAction));
		}

		/// <inheritdoc />
		public virtual void WriteTo(TextWriter writer)
		{
			if (writer == null) throw new ArgumentNullException(nameof(writer));

			_writeAction(writer).GetAwaiter().GetResult();
		}
	}
}
