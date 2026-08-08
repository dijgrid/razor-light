using System;
using System.IO;

namespace RazorLight.Text
{
	/// <summary>
	/// Defines the required contract for implementing an unencoded string.
	/// </summary>
	public interface IRawString : ITemplateContent
	{
	}

	/// <summary>
	/// Represents an unencoded string.
	/// </summary>
	public class RawString : IRawString
	{
		private readonly string _value;

		/// <summary>
		/// Initialises a new instance of <see cref="RawString"/>
		/// </summary>
		/// <param name="value">The value</param>
		public RawString(string value)
		{
			_value = value ?? string.Empty;
		}

		public string Value => _value;

		public void WriteTo(TextWriter writer)
		{
			if (writer == null)
			{
				throw new ArgumentNullException(nameof(writer));
			}

			writer.Write(Value);
		}
	}
}
