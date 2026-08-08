using System;
using System.Linq.Expressions;
using System.Reflection;

namespace RazorLight.Compilation
{
	internal sealed class TemplateFactoryProvider : ITemplateFactoryProvider
	{
		public Func<ITemplatePage> CreateFactory(CompiledTemplateDescriptor templateDescriptor)
		{
			string templateKey = templateDescriptor.TemplateKey
				?? throw new RazorLightException("The compiled template has no key.");

			if (templateDescriptor.TemplateAttribute != null)
			{
				Type compiledType = templateDescriptor.TemplateAttribute.TemplateType;

				var newExpression = Expression.New(compiledType);
				var keyProperty = compiledType.GetTypeInfo().GetProperty(nameof(ITemplatePage.Key));
				var propertyBindExpression = Expression.Bind(
					keyProperty ?? throw new RazorLightException($"Template type {compiledType} has no Key property."),
					Expression.Constant(templateKey));
				var objectInitializeExpression = Expression.MemberInit(newExpression, propertyBindExpression);

				var pageFactory = Expression
						.Lambda<Func<ITemplatePage>>(objectInitializeExpression)
						.Compile();

				return pageFactory;
			}
			else
			{
				throw new RazorLightException($"Template {templateKey} is corrupted or invalid");
			}
		}
	}
}
