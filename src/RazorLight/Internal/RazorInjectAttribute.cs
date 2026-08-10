using System;

namespace RazorLight
{
	/// <summary>Marks a generated template property for resolution from the render service scope.</summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public sealed class RazorInjectAttribute : Attribute
	{

	}
}
