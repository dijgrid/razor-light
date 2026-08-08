using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace RazorLight.Tests.Compatibility
{
	public class PublicApiBaselineTest
	{
		private static readonly NullabilityInfoContext NullabilityContext = new NullabilityInfoContext();

		[Fact]
		public void Public_API_matches_baseline()
		{
			var api = typeof(RazorLightEngine).Assembly
				.GetExportedTypes()
				.OrderBy(type => type.FullName, StringComparer.Ordinal)
				.Select(FormatTypeDeclaration);
			var apiText = string.Join("\n\n", api);
			string actualHash;
			using (var sha256 = SHA256.Create())
			{
				actualHash = BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(apiText)))
					.Replace("-", string.Empty)
					.ToLowerInvariant();
			}

			Assert.True(
				actualHash == "bd51f5341e4177f9c939c8bca864a14c13f5a6e875e6b5188c8fcd83be459c6c",
				"Actual public API hash: " + actualHash);
		}

		private static string FormatTypeDeclaration(Type type)
		{
			var builder = new StringBuilder();
			builder.Append(GetTypeKind(type));
			builder.Append(' ');
			builder.Append(FormatType(type));

			var inheritedTypes = GetInheritedTypes(type).ToArray();
			if (inheritedTypes.Length > 0)
			{
				builder.Append(" : ");
				builder.Append(string.Join(", ", inheritedTypes.Select(type => FormatType(type))));
			}

			var members = GetApiMembers(type).OrderBy(member => member, StringComparer.Ordinal).ToArray();
			if (members.Length == 0)
			{
				builder.Append("\n");
				builder.Append("{");
				builder.Append("\n");
				builder.Append("}");
				return builder.ToString();
			}

			builder.Append("\n");
			builder.Append("{");
			foreach (var member in members)
			{
				builder.Append("\n");
				builder.Append("    ");
				builder.Append(member);
			}

			builder.Append("\n");
			builder.Append("}");
			return builder.ToString();
		}

		private static IEnumerable<Type> GetInheritedTypes(Type type)
		{
			if (type.BaseType != null && type.BaseType != typeof(object) && type.BaseType != typeof(ValueType))
			{
				yield return type.BaseType;
			}

			foreach (var implementedInterface in type.GetInterfaces().OrderBy(item => item.FullName, StringComparer.Ordinal))
			{
				yield return implementedInterface;
			}
		}

		private static IEnumerable<string> GetApiMembers(Type type)
		{
			const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
				BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

			foreach (var constructor in type.GetConstructors(flags).Where(IsVisible))
			{
				yield return FormatVisibility(constructor) + " " + FormatType(type) +
					"(" + FormatParameters(constructor.GetParameters()) + ");";
			}

			foreach (var field in type.GetFields(flags).Where(IsVisible))
			{
				var modifiers = field.IsLiteral ? " const" : field.IsStatic ? " static" : string.Empty;
				var value = field.IsLiteral ? " = " + FormatValue(field.GetRawConstantValue()) : string.Empty;
				yield return FormatVisibility(field) + modifiers + " " + FormatType(field.FieldType, NullabilityContext.Create(field)) +
					" " + field.Name + value + ";";
			}

			foreach (var property in type.GetProperties(flags).Where(IsVisible))
			{
				var accessor = property.GetMethod ?? property.SetMethod;
				var modifiers = accessor != null && accessor.IsStatic ? " static" : string.Empty;
				var parameters = property.GetIndexParameters();
				var name = parameters.Length == 0
					? property.Name
					: "this[" + FormatParameters(parameters) + "]";
				var accessors = new List<string>();
				if (property.GetMethod != null && IsVisible(property.GetMethod)) accessors.Add("get;");
				if (property.SetMethod != null && IsVisible(property.SetMethod)) accessors.Add("set;");
				yield return FormatVisibility(accessor) + modifiers + " " + FormatType(property.PropertyType, NullabilityContext.Create(property)) +
					" " + name + " { " + string.Join(" ", accessors) + " }";
			}

			foreach (var eventInfo in type.GetEvents(flags).Where(IsVisible))
			{
				var accessor = eventInfo.AddMethod ?? eventInfo.RemoveMethod;
				var modifiers = accessor != null && accessor.IsStatic ? " static" : string.Empty;
				yield return FormatVisibility(accessor) + modifiers + " event " +
					FormatType(eventInfo.EventHandlerType, NullabilityContext.Create(eventInfo)) + " " + eventInfo.Name + ";";
			}

			foreach (var method in type.GetMethods(flags).Where(method => !method.IsSpecialName && IsVisible(method)))
			{
				var modifiers = method.IsStatic ? " static" : method.IsAbstract ? " abstract" :
					method.IsVirtual && !method.IsFinal ? " virtual" : string.Empty;
				var genericArguments = method.IsGenericMethodDefinition
					? "<" + string.Join(", ", method.GetGenericArguments().Select(argument => argument.Name)) + ">"
					: string.Empty;
				yield return FormatVisibility(method) + modifiers + " " +
					FormatType(method.ReturnType, NullabilityContext.Create(method.ReturnParameter)) +
					" " + method.Name + genericArguments + "(" + FormatParameters(method.GetParameters()) + ");";
			}
		}

		private static string FormatParameters(IEnumerable<ParameterInfo> parameters)
		{
			return string.Join(", ", parameters.Select(parameter =>
			{
				Type? parameterType = parameter.ParameterType;
				var modifier = parameter.GetCustomAttributes(typeof(ParamArrayAttribute), false).Any()
					? "params "
					: parameterType.IsByRef ? parameter.IsOut ? "out " : "ref " : string.Empty;
				if (parameterType.IsByRef) parameterType = parameterType.GetElementType();
				var defaultValue = parameter.HasDefaultValue ? " = " + FormatValue(parameter.DefaultValue) : string.Empty;
				return modifier + FormatType(parameterType, NullabilityContext.Create(parameter)) + " " + parameter.Name + defaultValue;
			}));
		}

		private static string FormatType(Type? type, NullabilityInfo? nullability = null)
		{
			if (type == null) return "null";
			var suffix = IsNullable(type, nullability) ? "?" : string.Empty;
			if (type.IsArray) return FormatType(type.GetElementType(), nullability?.ElementType) + "[]" + suffix;
			if (type.IsGenericParameter) return type.Name + suffix;

			var nullableType = Nullable.GetUnderlyingType(type);
			if (nullableType != null) return FormatType(nullableType) + "?";

			var name = type.FullName ?? type.Name;
			var tickIndex = name.IndexOf('`');
			if (tickIndex >= 0) name = name.Substring(0, tickIndex);
			name = name.Replace('+', '.');

			if (!type.IsGenericType) return name + suffix;
			var genericNullability = nullability?.GenericTypeArguments ?? Array.Empty<NullabilityInfo>();
			var genericArguments = type.GetGenericArguments();
			var formattedArguments = genericArguments.Select((argument, index) =>
				FormatType(argument, index < genericNullability.Length ? genericNullability[index] : null));
			return name + "<" + string.Join(", ", formattedArguments) + ">" + suffix;
		}

		private static bool IsNullable(Type type, NullabilityInfo? nullability)
		{
			return Nullable.GetUnderlyingType(type) == null &&
				!type.IsValueType &&
				(nullability?.ReadState == NullabilityState.Nullable ||
				 nullability?.WriteState == NullabilityState.Nullable);
		}

		private static string GetTypeKind(Type type)
		{
			if (type.IsEnum) return "enum";
			if (type.IsInterface) return "interface";
			if (type.BaseType != null && typeof(MulticastDelegate).IsAssignableFrom(type.BaseType)) return "delegate";
			if (type.IsValueType) return "struct";
			return type.IsAbstract && type.IsSealed ? "static class" : type.IsAbstract ? "abstract class" : "class";
		}

		private static bool IsVisible(MethodBase? method)
		{
			return method != null && (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly);
		}

		private static bool IsVisible(FieldInfo field)
		{
			return field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly;
		}

		private static bool IsVisible(PropertyInfo property)
		{
			return IsVisible(property.GetMethod) || IsVisible(property.SetMethod);
		}

		private static bool IsVisible(EventInfo eventInfo)
		{
			return IsVisible(eventInfo.AddMethod) || IsVisible(eventInfo.RemoveMethod);
		}

		private static string FormatVisibility(MethodBase? method)
		{
			if (method == null) return "public";
			if (method.IsPublic) return "public";
			if (method.IsFamilyOrAssembly) return "protected internal";
			return "protected";
		}

		private static string FormatVisibility(FieldInfo field)
		{
			if (field.IsPublic) return "public";
			if (field.IsFamilyOrAssembly) return "protected internal";
			return "protected";
		}

		private static string FormatValue(object? value)
		{
			if (value == null || value == DBNull.Value || value == Missing.Value) return "null";
			if (value is string stringValue) return "\"" + stringValue.Replace("\"", "\\\"") + "\"";
			if (value is char) return "'" + value + "'";
			if (value is bool) return (bool)value ? "true" : "false";
			return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
		}
	}
}
