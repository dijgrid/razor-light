using System.IO;

namespace RazorLight.Text
{
	/// <summary>
	/// Transforms expression values before they are written to template output.
	/// </summary>
	public interface IOutputEncoder
	{
		void Encode(TextWriter writer, string value);
	}
}
