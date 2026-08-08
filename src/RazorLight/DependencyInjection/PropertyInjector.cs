using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using RazorLight.Internal;

namespace RazorLight.DependencyInjection
{
	internal sealed class PropertyInjector
	{
		private readonly ConditionalWeakTable<Type, InjectionPlan> _plans = new();
		private int _planCreationCount;

		internal int PlanCreationCount => Volatile.Read(ref _planCreationCount);

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

			InjectionPlan plan = _plans.GetValue(page.GetType(), CreatePlan);
			foreach (InjectionProperty property in plan.Properties)
			{
				object instance = services.GetRequiredService(property.ServiceType);
				property.Setter.SetValue(page, instance);
			}
		}

		private InjectionPlan CreatePlan(Type pageType)
		{
			Interlocked.Increment(ref _planCreationCount);
			var properties = pageType.GetRuntimeProperties()
				.Where(property =>
					property.IsDefined(typeof(RazorInjectAttribute)) &&
					property.GetIndexParameters().Length == 0 &&
					property.SetMethod?.IsStatic == false)
				.Select(property => new InjectionProperty(property.PropertyType, new FastPropertySetter(property)))
				.ToArray();
			return new InjectionPlan(properties);
		}

		private sealed record InjectionPlan(InjectionProperty[] Properties);
		private sealed record InjectionProperty(Type ServiceType, FastPropertySetter Setter);
	}
}
