using System;

namespace RazorLight
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public sealed class RazorInjectAttribute : Attribute
	{

	}
}
