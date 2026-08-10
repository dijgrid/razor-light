using System;

namespace RazorLight
{
	/// <summary>Base exception for RazorLight configuration, lookup, generation, and compilation failures.</summary>
	public class RazorLightException : Exception
	{
		/// <summary>Creates an exception without a message.</summary>
		public RazorLightException()
		{
		}

		/// <summary>Creates an exception with a descriptive message.</summary>
		public RazorLightException(string message) : base(message) { }

		/// <summary>Creates an exception with a descriptive message and underlying cause.</summary>
		public RazorLightException(string message, Exception exception) : base(message, exception) { }
	}
}
