using System;
using System.Text.Encodings.Web;

namespace RazorLight.Html
{
	/// <summary>Adds optional HTML expression encoding to a RazorLight engine builder.</summary>
	public static class RazorLightEngineBuilderExtensions
	{
		/// <summary>
		/// Escapes template expression values for HTML output.
		/// </summary>
		public static RazorLightEngineBuilder UseHtmlEncoding(
			this RazorLightEngineBuilder builder,
			HtmlEncoder? encoder = null)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			return builder.UseOutputEncoder(new HtmlOutputEncoder(encoder));
		}
	}
}
