using System.IO;

namespace RazorLight.Text
{
	/// <summary>
	/// Transforms expression values before they are written to template output.
	/// </summary>
	public interface IOutputEncoder
	{
		/// <summary>Writes a transformed Razor expression value to the destination writer.</summary>
		void Encode(TextWriter writer, string value);
	}
}
