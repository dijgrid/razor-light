using System;

namespace RazorLight.Compilation
{
	internal sealed class UnavailableTemplateFactoryProvider : ITemplateFactoryProvider
	{
		public Func<ITemplatePage> CreateFactory(CompiledTemplateDescriptor templateDescriptor) =>
			throw new RazorLightException("Precompiled-only mode requires page factories supplied by its cache provider.");
	}
}
