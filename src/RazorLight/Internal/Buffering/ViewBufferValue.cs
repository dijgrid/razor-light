using System.Diagnostics;
using System.IO;
using RazorLight.Text;

namespace RazorLight.Internal.Buffering
{
	/// <summary>
	/// Encapsulates a string or <see cref="ITemplateContent"/> value.
	/// </summary>
	[DebuggerDisplay("{DebuggerToString()}")]
	public struct ViewBufferValue
	{
		/// <summary>
		/// Initializes a new instance of <see cref="ViewBufferValue"/> with a <c>string</c> value.
		/// </summary>
		/// <param name="value">The value.</param>
		public ViewBufferValue(string value)
		{
			Value = value;
		}

		/// <summary>
		/// Initializes a new instance of <see cref="ViewBufferValue"/> with an <see cref="ITemplateContent"/> value.
		/// </summary>
		/// <param name="content">The final template content.</param>
		public ViewBufferValue(ITemplateContent content)
		{
			Value = content;
		}

		/// <summary>
		/// Gets the value.
		/// </summary>
		public object? Value { get; }

		private string DebuggerToString()
		{
			using (var writer = new StringWriter())
			{
				var valueAsString = Value as string;
				if (valueAsString != null)
				{
					writer.Write(valueAsString);
					return writer.ToString();
				}

				var valueAsContent = Value as ITemplateContent;
				if (valueAsContent != null)
				{
					valueAsContent.WriteTo(writer);
					return writer.ToString();
				}

				return "(null)";
			}
		}
	}
}
