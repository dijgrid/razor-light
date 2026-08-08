using System;
using Microsoft.Extensions.Primitives;
using RazorLight.Razor;

namespace RazorLight.Compilation
{
	internal sealed class CompiledTemplateDescriptor
	{
		public string? TemplateKey { get; set; }

		public RazorLightTemplateAttribute? TemplateAttribute { get; set; }

		public IChangeToken? ExpirationToken { get; set; }

		public bool IsPrecompiled { get; set; }

		/// <summary>
		/// Gets the type of the compiled item.
		/// </summary>
		public Type? Type => TemplateAttribute?.TemplateType;
	}
}
