using System;
using System.IO;
using System.Text;

namespace RazorLight.Razor
{
	/// <inheritdoc />
	public sealed class TextSourceRazorProjectItem : RazorLightProjectItem
	{
		private string _content;

		/// <inheritdoc />
		public TextSourceRazorProjectItem(string key, string content)
		{
			Key = key ?? throw new ArgumentNullException(nameof(key));

			_content = content ?? throw new ArgumentNullException(nameof(content));
		}

		/// <inheritdoc />
		public override string Key { get; }

		/// <inheritdoc />
		public override bool Exists => true;

		/// <inheritdoc />
		public string Content => _content;

		/// <inheritdoc />
		public override Stream Read()
		{
			return new MemoryStream(Encoding.UTF8.GetBytes(_content));
		}
	}
}
