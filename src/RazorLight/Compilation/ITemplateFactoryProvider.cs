using System;

namespace RazorLight.Compilation
{
	internal interface ITemplateFactoryProvider
	{
		Func<ITemplatePage> CreateFactory(CompiledTemplateDescriptor templateDescriptor);
	}
}
