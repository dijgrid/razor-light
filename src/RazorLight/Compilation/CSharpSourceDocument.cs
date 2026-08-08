using Microsoft.Extensions.Primitives;
using System.Collections.Generic;

namespace RazorLight.Compilation
{
	internal sealed class CSharpSourceDocument
	{
		public CSharpSourceDocument(string key, string content, IChangeToken? expirationToken)
		{
			Key = key;
			Content = content;
			ExpirationToken = expirationToken;
		}

		public string Key { get; }

		public string Content { get; }

		public IChangeToken? ExpirationToken { get; }
	}

	internal interface IGeneratedCSharpSourceContainer
	{
		IReadOnlyList<CSharpSourceDocument> CSharpSources { get; }
	}
}
