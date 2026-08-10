using System;
using System.IO;

namespace RazorLight.Razor
{
	/// <inheritdoc />
	public sealed class FileSystemRazorProjectItem : RazorLightProjectItem
	{
		/// <inheritdoc />
		public FileSystemRazorProjectItem(string templateKey, FileInfo fileInfo)
		{
			Key = templateKey ?? throw new ArgumentNullException(nameof(templateKey));
			File = fileInfo ?? throw new ArgumentNullException(nameof(fileInfo));
		}

		/// <inheritdoc />
		public FileInfo File { get; }

		/// <inheritdoc />
		public override string Key { get; }

		/// <inheritdoc />
		public override bool Exists => File.Exists;

		/// <inheritdoc />
		public override Stream Read() => File.OpenRead();
	}
}
