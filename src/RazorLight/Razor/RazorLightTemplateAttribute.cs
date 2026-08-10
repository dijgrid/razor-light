using System;
using System.Diagnostics.CodeAnalysis;

namespace RazorLight.Razor
{
	/// <summary>Identifies the generated page and compatibility metadata in a precompiled template assembly.</summary>
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
	public sealed class RazorLightTemplateAttribute : Attribute
	{
		/// <inheritdoc />
		public const int CurrentFormatVersion = 1;
		/// <inheritdoc />
		public const string CurrentCompilerVersion = "RazorLight-3.0-Razor6";

		/// <inheritdoc />
		public RazorLightTemplateAttribute(
			string key,
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties)] Type templateType)
			: this(key, templateType, CurrentFormatVersion, CurrentCompilerVersion, "unspecified", "unspecified")
		{
		}

		/// <inheritdoc />
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

		/// <inheritdoc />
		public int FormatVersion { get; }
		/// <inheritdoc />
		public string CompilerVersion { get; }
		/// <inheritdoc />
		public string ModelContract { get; }
		/// <inheritdoc />
		public string SourceChecksum { get; }
	}
}
