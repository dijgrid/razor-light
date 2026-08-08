using System.Reflection;

namespace RazorLight.Compilation
{
	internal sealed class DefaultAssemblyPathFormatter : IAssemblyPathFormatter
	{
		public string GetAssemblyPath(Assembly assembly) => assembly.IsDynamic ? string.Empty : assembly.Location;
	}
}
