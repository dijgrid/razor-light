using System.IO;

namespace RazorLight.Text
{
	/// <summary>
	/// Represents template output that is already in its final form and must not be transformed.
	/// </summary>
	public interface ITemplateContent
	{
		/// <summary>Writes final template content without applying expression encoding again.</summary>
		void WriteTo(TextWriter writer);
	}
}
