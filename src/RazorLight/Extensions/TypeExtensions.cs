using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace RazorLight.Extensions
{
	public static class TypeExtensions
	{
		[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Anonymous-object conversion is an opt-in reflection feature; callers must preserve model properties.")]
		public static ExpandoObject ToExpando(this object anonymousObject)
		{
			if (anonymousObject is ExpandoObject exp)
			{
				return exp;
			}

			IDictionary<string, object?> expando = new ExpandoObject();
			foreach (var propertyDescriptor in anonymousObject.GetType().GetTypeInfo().GetProperties())
			{
				var obj = propertyDescriptor.GetValue(anonymousObject);
				if (obj != null && obj.GetType().IsAnonymousType())
				{
					obj = obj.ToExpando();
				}
				expando.Add(propertyDescriptor.Name, obj);
			}

			return (ExpandoObject)expando;
		}

		public static bool IsAnonymousType(this Type type)
		{
			bool hasCompilerGeneratedAttribute = type.GetTypeInfo()
				.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false)
				.Any();

			bool nameContainsAnonymousType = type.FullName?.Contains("AnonymousType") == true;
			bool isAnonymousType = hasCompilerGeneratedAttribute && nameContainsAnonymousType;

			return isAnonymousType;
		}

	}
}
