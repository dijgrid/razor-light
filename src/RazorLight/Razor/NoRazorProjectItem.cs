using System;
using System.IO;

namespace RazorLight.Razor
{
	/// <inheritdoc />
	public sealed class NoRazorProjectItem : RazorLightProjectItem
	{
		private NoRazorProjectItem()
		{
		}

		private static readonly Lazy<NoRazorProjectItem> EmptyImpl = new Lazy<NoRazorProjectItem>(() => new NoRazorProjectItem());
		/// <inheritdoc />
		public static NoRazorProjectItem Empty => EmptyImpl.Value;

		/// <inheritdoc />
		public override string Key => string.Empty;
		/// <inheritdoc />
		public override bool Exists { get; }

		/// <inheritdoc />
		public override Stream Read()
		{
			throw new NotImplementedException($"{nameof(NoRazorProjectItem)} is only used by string templates.");
		}

		/// <inheritdoc />
		public override bool Equals(object? obj)
		{
			var other = obj as NoRazorProjectItem;
			return string.Equals(Key, other?.Key);
		}

		private bool Equals(NoRazorProjectItem? other)
		{
			return Key == other?.Key;
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			return (Key != null ? Key.GetHashCode() : 0);
		}
	}
}
