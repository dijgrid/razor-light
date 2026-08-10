using System;
using System.Collections.Generic;

namespace RazorLight
{
	/// <summary>Indicates that no dynamic or project template exists for a requested key.</summary>
	public class TemplateNotFoundException : RazorLightException
	{
		/// <summary>Creates a missing-template error without detailed known-key inventories.</summary>
		public TemplateNotFoundException(string message) : base(message) { }

		/// <summary>Creates a missing-template error with an underlying lookup failure.</summary>
		public TemplateNotFoundException(string message, Exception exception) : base(message, exception) { }

		/// <summary>Creates a detailed missing-template error with development-only known-key inventories.</summary>
		public TemplateNotFoundException(
			string message,
			IEnumerable<string> knownDynamicTemplateKeys,
			IEnumerable<string> knownProjectTemplateKeys) : base(message)
		{
			KnownDynamicTemplateKeys = knownDynamicTemplateKeys;
			KnownProjectTemplateKeys = knownProjectTemplateKeys;
		}

		/// <summary>
		/// The known template keys of any dynamically created templates.
		/// Only set when <c>RazorLightOptions.DebugMode = true</c>
		/// </summary>
		public IEnumerable<string>? KnownDynamicTemplateKeys { get; }

		/// <summary>
		/// The known template keys by the associated <c>RazorLightProject</c>.
		/// Only set when <c>RazorLightOptions.DebugMode = true</c>
		/// </summary>
		public IEnumerable<string>? KnownProjectTemplateKeys { get; }
	}
}
