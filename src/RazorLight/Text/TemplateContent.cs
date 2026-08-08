using System;
using System.IO;

namespace RazorLight.Text
{
	/// <summary>
	/// Represents content that bypasses the configured output encoder.
	/// </summary>
	public sealed class TemplateContent : ITemplateContent
	{
		public static TemplateContent Empty { get; } = new TemplateContent(string.Empty);

		public TemplateContent(string value)
		{
			Value = value ?? throw new ArgumentNullException(nameof(value));
		}

		public string Value { get; }

		public void WriteTo(TextWriter writer)
		{
			if (writer == null) throw new ArgumentNullException(nameof(writer));

			writer.Write(Value);
		}

		public override string ToString() => Value;
	}
}
