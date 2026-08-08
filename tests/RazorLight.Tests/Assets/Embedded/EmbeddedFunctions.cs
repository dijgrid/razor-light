using System.Linq;

namespace EmbeddedComposition;

internal static class EmbeddedFunctions
{
	internal static string Reverse(string value) => new string(value.Reverse().ToArray());
}
