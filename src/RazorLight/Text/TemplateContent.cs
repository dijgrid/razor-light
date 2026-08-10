using System;
using System.IO;

namespace RazorLight.Text
{
	/// <summary>
	/// Represents content that bypasses the configured output encoder.
	/// </summary>
	public sealed class TemplateContent : ITemplateContent
	{
		/// <inheritdoc />
		public static TemplateContent Empty { get; } = new TemplateContent(string.Empty);

		/// <inheritdoc />
		public TemplateContent(string value)
		{
			Value = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <inheritdoc />
		public string Value { get; }

		/// <inheritdoc />
		public void WriteTo(TextWriter writer)
		{
			if (writer == null) throw new ArgumentNullException(nameof(writer));

			writer.Write(Value);
		}

		/// <inheritdoc />
		public override string ToString() => Value;
	}
}
