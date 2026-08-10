using System.IO;
using Microsoft.Extensions.Primitives;

namespace RazorLight.Razor
{
	/// <summary>Represents one logical Razor, import, or C# source item returned by a project.</summary>
	public abstract class RazorLightProjectItem
	{
		/// <summary>Gets or sets a token that invalidates compiled dependents when this item changes.</summary>
		public IChangeToken? ExpirationToken { get; set; }

		/// <summary>
		/// Unique key of the template that was searched
		/// </summary>
		public abstract string Key { get; }

		/// <summary>
		/// Gets if template exists
		/// </summary>
		public abstract bool Exists { get; }


		/// <summary>
		/// Returns 
		/// </summary>
		/// <returns></returns>
		public abstract Stream Read();
	}
}
