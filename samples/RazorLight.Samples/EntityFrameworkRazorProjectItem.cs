using RazorLight.Razor;
using System;
using System.IO;
using System.Text;

namespace Samples.EntityFrameworkProject
{
	public class EntityFrameworkRazorProjectItem : RazorLightProjectItem
	{
		private readonly string? _content;

		public EntityFrameworkRazorProjectItem(string key, string? content)
		{
			Key = key;
			_content = content;
		}

		public override string Key { get; }

		public override bool Exists => _content != null;

		public override Stream Read()
		{
			var content = _content
				?? throw new InvalidOperationException("Cannot read a template that does not exist.");

			return new MemoryStream(Encoding.UTF8.GetBytes(content));
		}
	}
}
