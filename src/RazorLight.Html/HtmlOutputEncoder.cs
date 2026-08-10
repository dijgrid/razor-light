using System;
using System.IO;
using System.Text.Encodings.Web;
using RazorLight.Text;

namespace RazorLight.Html
{
	/// <summary>
	/// Applies HTML escaping to Razor expression values.
	/// </summary>
	public sealed class HtmlOutputEncoder : IOutputEncoder
	{
		private readonly HtmlEncoder _encoder;

		/// <summary>Creates an output encoder using the supplied HTML policy or the platform default.</summary>
		public HtmlOutputEncoder(HtmlEncoder? encoder = null)
		{
			_encoder = encoder ?? HtmlEncoder.Default;
		}

		/// <inheritdoc />
		public void Encode(TextWriter writer, string value)
		{
			if (writer == null) throw new ArgumentNullException(nameof(writer));
			if (value == null) throw new ArgumentNullException(nameof(value));

			_encoder.Encode(writer, value);
		}
	}
}
