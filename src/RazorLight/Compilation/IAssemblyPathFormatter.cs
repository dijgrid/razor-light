using System.Reflection;

namespace RazorLight.Compilation
{
	internal interface IAssemblyPathFormatter
	{
		string GetAssemblyPath(Assembly assembly);
	}
}
