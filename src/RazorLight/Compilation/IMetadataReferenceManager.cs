using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace RazorLight.Compilation
{
	internal interface IMetadataReferenceManager
	{
		IReadOnlyList<MetadataReference> Resolve(Assembly assembly);

		HashSet<MetadataReference> AdditionalMetadataReferences { get; }
	}
}
