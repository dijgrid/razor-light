using System;
using System.Diagnostics.CodeAnalysis;

namespace RazorLight.Razor
{
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
	public sealed class RazorLightTemplateAttribute : Attribute
	{
		public const int CurrentFormatVersion = 1;
		public const string CurrentCompilerVersion = "RazorLight-3.0-Razor6";

		public RazorLightTemplateAttribute(
			string key,
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties)] Type templateType)
			: this(key, templateType, CurrentFormatVersion, CurrentCompilerVersion, "unspecified", "unspecified")
		{
		}

		public RazorLightTemplateAttribute(
			string key,
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties)] Type templateType,
			int formatVersion,
			string compilerVersion,
			string modelContract,
			string sourceChecksum)
		{
			Key = key;
			TemplateType = templateType;
			FormatVersion = formatVersion;
			CompilerVersion = compilerVersion;
			ModelContract = modelContract;
			SourceChecksum = sourceChecksum;
		}

		/// <summary>
		/// Gets the key of the view.
		/// </summary>
		public string Key { get; }

		/// <summary>
		/// Gets the template type.
		/// </summary>
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties)]
		public Type TemplateType { get; }

		public int FormatVersion { get; }
		public string CompilerVersion { get; }
		public string ModelContract { get; }
		public string SourceChecksum { get; }
	}
}
