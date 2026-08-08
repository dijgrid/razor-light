using RazorLight.Extensions;
using System;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace RazorLight
{
	/// <summary>
	/// Stores information about model of the template page
	/// </summary>
	public sealed class ModelTypeInfo
	{
		/// <summary>
		/// Indicates whether given model is not a dynamic or anonymous object
		/// </summary>
		public bool IsStrongType { get; private set; }

		/// <summary>
		/// Real type of the model
		/// </summary>
		public Type Type { get; private set; }

		/// <summary>
		/// Type that will be used on compilation of the template.
		/// If <see cref="Type"/> is anonymous or dynamic - <see cref="TemplateType"/> becomes <see cref="ExpandoObject"/>
		/// </summary>
		public Type TemplateType { get; private set; }

		/// <summary>
		/// Name of the type that will be used on compilation of the template
		/// </summary>
		public string TemplateTypeName { get; private set; }

		/// <summary>
		/// Transforms object into template type
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		public object CreateTemplateModel(object model)
		{
			return this.IsStrongType ? model : model.ToExpando();
		}

		public ModelTypeInfo(Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException(nameof(type));
			}

			this.Type = type;
			this.IsStrongType = type != typeof(ExpandoObject) && !Type.IsAnonymousType();
			this.TemplateType = IsStrongType ? Type : typeof(ExpandoObject);
			this.TemplateTypeName = IsStrongType ? GetFriendlyName(Type) : "dynamic";
		}

		public static string GetFriendlyName(Type type)
		{
			if (type.IsArray)
			{
				return GetFriendlyName(type.GetElementType()
					?? throw new ArgumentException("The array type has no element type.", nameof(type)))
					+ "[" + new string(',', type.GetArrayRank() - 1) + "]";
			}

			if (type.IsGenericParameter)
			{
				return type.Name;
			}

			string name = type.Name.Split('`')[0];
			string prefix = type.IsNested
				? GetFriendlyName(type.DeclaringType
					?? throw new ArgumentException("The nested type has no declaring type.", nameof(type))) + "." + name
				: string.IsNullOrEmpty(type.Namespace) ? name : type.Namespace + "." + name;

			if (!IsGenericType(type))
			{
				return prefix;
			}

			int declaringArgumentCount = type.DeclaringType?.GetGenericArguments().Length ?? 0;
			Type[] ownArguments = type.GetGenericArguments().Skip(declaringArgumentCount).ToArray();
			return ownArguments.Length == 0
				? prefix
				: prefix + "<" + string.Join(", ", ownArguments.Select(GetFriendlyName)) + ">";
		}

		private static bool IsGenericType(Type type)
		{
			return type.GetTypeInfo().IsGenericType;
		}
	}
}
