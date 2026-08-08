using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using RazorLight.Internal;

namespace RazorLight.DependencyInjection
{
	internal sealed class PropertyInjector
	{
		private readonly ConcurrentDictionary<PropertyInfo, FastPropertySetter> _propertyCache;

		public PropertyInjector()
		{
			this._propertyCache = new ConcurrentDictionary<PropertyInfo, FastPropertySetter>();
		}

		public void Inject(ITemplatePage page, IServiceProvider services)
		{
			if (page == null)
			{
				throw new ArgumentNullException(nameof(page));
			}
			if (services == null)
			{
				throw new ArgumentNullException(nameof(services));
			}

			PropertyInfo[] properties = page.GetType().GetRuntimeProperties()
			   .Where(p =>
			   {
				   return
					   p.IsDefined(typeof(RazorInjectAttribute)) &&
					   p.GetIndexParameters().Length == 0 &&
					   p.SetMethod?.IsStatic == false;
			   }).ToArray();

			foreach (var property in properties)
			{
				Type memberType = property.PropertyType;
				object instance = services.GetRequiredService(memberType);

				FastPropertySetter setter = _propertyCache.GetOrAdd(property, new FastPropertySetter(property));
				setter.SetValue(page, instance);
			}
		}
	}
}
