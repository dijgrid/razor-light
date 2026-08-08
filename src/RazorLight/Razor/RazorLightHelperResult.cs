using System;
using System.IO;
using System.Threading.Tasks;
using RazorLight.Text;

namespace RazorLight.Razor
{
	public class RazorLightHelperResult : ITemplateContent
	{
		private readonly Func<TextWriter, Task> _writeAction;

		public RazorLightHelperResult(Func<TextWriter, Task> asyncAction)
		{
			_writeAction = asyncAction ?? throw new ArgumentNullException(nameof(asyncAction));
		}

		public virtual void WriteTo(TextWriter writer)
		{
			if (writer == null) throw new ArgumentNullException(nameof(writer));

			_writeAction(writer).GetAwaiter().GetResult();
		}
	}
}
