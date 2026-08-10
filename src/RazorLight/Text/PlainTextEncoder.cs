using System;
using System.IO;

namespace RazorLight.Text
{
	/// <summary>
	/// Writes expression values without format-specific escaping.
	/// </summary>
	public sealed class PlainTextEncoder : IOutputEncoder
	{
		/// <inheritdoc />
		public static PlainTextEncoder Default { get; } = new PlainTextEncoder();

		private PlainTextEncoder()
		{
		}

		/// <inheritdoc />
		public void Encode(TextWriter writer, string value)
		{
			if (writer == null) throw new ArgumentNullException(nameof(writer));
			if (value == null) throw new ArgumentNullException(nameof(value));

			writer.Write(value);
		}
	}
}
