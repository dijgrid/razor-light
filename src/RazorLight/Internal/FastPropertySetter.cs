using System;
using System.Linq.Expressions;
using System.Reflection;

namespace RazorLight.Internal
{
	internal sealed class FastPropertySetter
	{
		private readonly Action<object, object?> _valueSetter;

		public FastPropertySetter(PropertyInfo property)
		{
			Property = property ?? throw new ArgumentNullException(nameof(property));
			if (property.SetMethod == null || property.SetMethod.IsStatic || property.GetIndexParameters().Length != 0)
			{
				throw new ArgumentException($"Property '{property.Name}' must have an instance setter and no index parameters.", nameof(property));
			}

			Name = property.Name;
			_valueSetter = MakeFastPropertySetter(property);
		}

		public string Name { get; }
		public PropertyInfo Property { get; }
		public Action<object, object?> ValueSetter => _valueSetter;

		public void SetValue(object instance, object? value) => _valueSetter(instance, value);

		public static Action<object, object?> MakeFastPropertySetter(PropertyInfo propertyInfo)
		{
			if (propertyInfo == null) throw new ArgumentNullException(nameof(propertyInfo));
			MethodInfo setter = propertyInfo.SetMethod
				?? throw new ArgumentException($"Property '{propertyInfo.Name}' has no setter.", nameof(propertyInfo));
			if (setter.IsStatic || propertyInfo.GetIndexParameters().Length != 0)
			{
				throw new ArgumentException($"Property '{propertyInfo.Name}' must have an instance setter and no index parameters.", nameof(propertyInfo));
			}

			var instance = Expression.Parameter(typeof(object), "instance");
			var value = Expression.Parameter(typeof(object), "value");
			var call = Expression.Call(
				Expression.Convert(instance, propertyInfo.DeclaringType!),
				setter,
				Expression.Convert(value, propertyInfo.PropertyType));
			return Expression.Lambda<Action<object, object?>>(call, instance, value).Compile();
		}
	}
}
