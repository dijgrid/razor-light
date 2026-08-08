using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using RazorLight.Internal;
using RazorLight.Internal.Buffering;
using RazorLight.Text;

namespace RazorLight
{
	public abstract class TemplatePageBase : ITemplatePage
	{
		private readonly Stack<TextWriter> _textWriterStack = new Stack<TextWriter>();
		private AttributeInfo _attributeInfo;

		public abstract void SetModel(object? model);

		/// <inheritdoc />
		public virtual PageContext? PageContext { get; set; }

		/// <inheritdoc />
		public ITemplateContent? BodyContent { get; set; }

		/// <inheritdoc />
		public bool IsLayoutBeingRendered { get; set; }

		/// <inheritdoc />
		public string? Layout { get; set; }

		public virtual dynamic ViewBag
		{
			get
			{
				if (PageContext == null)
				{
					throw new InvalidOperationException();
				}

				return PageContext.ViewBag;
			}
		}

		public Func<string, object?, Task>? IncludeFunc { get; set; }

		/// <inheritdoc />
		public IDictionary<string, RenderAsyncDelegate>? PreviousSectionWriters { get; set; }

		/// <inheritdoc />
		public IDictionary<string, RenderAsyncDelegate> SectionWriters { get; } =
			new Dictionary<string, RenderAsyncDelegate>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Gets or sets the encoder used for expression values.
		/// </summary>
		public IOutputEncoder OutputEncoder { get; set; } = PlainTextEncoder.Default;

		/// <inheritdoc />
		public string? Key { get; set; }

		/// <summary>
		/// Gets the <see cref="TextWriter"/> that the template is writing output to.
		/// </summary>
		public virtual TextWriter Output
		{
			get
			{
				if (PageContext == null)
				{
					throw new InvalidOperationException();
				}

				return PageContext.Writer;
			}
		}


		/// <inheritdoc />
		public abstract Task ExecuteAsync();

		/// <summary>
		/// Invokes <see cref="TextWriter.FlushAsync()"/> on <see cref="Output"/> and <see cref="m:Stream.FlushAsync"/>
		/// on the output writer, writing out any buffered content.
		/// </summary>
		/// <returns>A task that represents the asynchronous flush operation and returns an empty content token.</returns>
		/// <remarks>The value returned is a token that allows <see cref="FlushAsync"/> to be used directly
		/// in a template section. It does not represent rendered content.</remarks>
		public virtual async Task<TemplateContent> FlushAsync()
		{
			// Calls to Flush are allowed if the page does not specify a Layout or if it is executing a section in the
			// Layout.
			if (!IsLayoutBeingRendered && !string.IsNullOrEmpty(Layout))
			{
				throw new InvalidOperationException();
			}

			await Output.FlushAsync();
			return TemplateContent.Empty;
		}

		public abstract void BeginContext(int position, int length, bool isLiteral);

		public abstract void EndContext();

		public abstract void EnsureRenderedBodyOrSections();

		/// <summary>
		/// Returns the specified string as final content that bypasses the output encoder.
		/// </summary>
		/// <param name="rawString">The raw string to write.</param>
		/// <returns>An instance of <see cref="IRawString"/>.</returns>
		public IRawString Raw(string rawString)
		{
			return new RawString(rawString);
		}

		public static ITemplateContent HelperFunction(Func<object?, ITemplateContent> body)
		{
			return body(null);
		}

		/// <summary>
		/// Creates a named content section in the page that can be invoked in a Layout page using
		/// <c>RenderSection</c> or <c>RenderSectionAsync</c>
		/// </summary>
		/// <param name="name">The name of the section to create.</param>
		/// <param name="section">The delegate to execute when rendering the section.</param>
		/// <remarks>This overload supports legacy Razor editor code generation.</remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected void DefineSection(string name, Func<object?, Task> section)
			=> DefineSection(name, () => section(null /* writer */));

		/// <summary>
		/// Creates a named content section in the page that can be invoked in a Layout page using
		/// <c>RenderSection</c> or <c>RenderSectionAsync</c>
		/// </summary>
		/// <param name="name">The name of the section to create.</param>
		/// <param name="section">The <see cref="RenderAsyncDelegate"/> to execute when rendering the section.</param>
		public virtual void DefineSection(string name, RenderAsyncDelegate section)
		{
			if (name == null)
			{
				throw new ArgumentNullException(nameof(name));
			}

			if (section == null)
			{
				throw new ArgumentNullException(nameof(section));
			}

			if (SectionWriters.ContainsKey(name))
			{
				throw new InvalidOperationException();
			}
			SectionWriters[name] = section;
		}

		#region Write section

		/// <summary>
		/// Writes the specified <paramref name="value"/> using the configured output encoder.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to write.</param>
		public virtual void Write(object? value)
		{
			if (value == null)
			{
				return;
			}

			var writer = Output;
			switch (value)
			{
				case ITemplateContent content:
					var bufferedWriter = writer as ViewBufferTextWriter;
					if (content is ViewBuffer contentBuffer && bufferedWriter?.IsBuffering == true)
					{
						contentBuffer.MoveTo(bufferedWriter.Buffer);
					}
					else
					{
						content.WriteTo(writer);
					}
					break;
				default:
					Write(value.ToString());
					break;
			}
		}

		/// <summary>
		/// Writes the specified <paramref name="value"/> using the configured output encoder.
		/// </summary>
		/// <param name="value">The <see cref="string"/> to write.</param>
		public virtual void Write(string? value)
		{
			var writer = Output;
			if (!string.IsNullOrEmpty(value))
			{
				OutputEncoder.Encode(writer, value);
			}
		}

		/// <summary>
		/// Writes the specified <paramref name="value"/> without applying the output encoder.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to write.</param>
		public virtual void WriteLiteral(object? value)
		{
			if (value == null)
			{
				return;
			}

			WriteLiteral(value.ToString());
		}

		/// <summary>
		/// Writes the specified <paramref name="value"/> without applying the output encoder.
		/// </summary>
		/// <param name="value">The <see cref="string"/> to write.</param>
		public virtual void WriteLiteral(string? value)
		{
			if (!string.IsNullOrEmpty(value))
			{
				Output.Write(value);
			}
		}

		// Internal for unit testing.
		protected internal virtual void PushWriter(TextWriter writer)
		{
			if (writer == null)
			{
				throw new ArgumentNullException(nameof(writer));
			}

			var pageContext = PageContext ?? throw new InvalidOperationException("PageContext is not set.");
			_textWriterStack.Push(pageContext.Writer);
			pageContext.Writer = writer;
		}

		// Internal for unit testing.
		protected internal virtual TextWriter PopWriter()
		{
			var pageContext = PageContext ?? throw new InvalidOperationException("PageContext is not set.");
			pageContext.Writer = _textWriterStack.Pop();
			return pageContext.Writer;
		}

		public virtual void BeginWriteAttribute(
			string name,
			string prefix,
			int prefixOffset,
			string suffix,
			int suffixOffset,
			int attributeValuesCount)
		{
			if (prefix == null)
			{
				throw new ArgumentNullException(nameof(prefix));
			}

			if (suffix == null)
			{
				throw new ArgumentNullException(nameof(suffix));
			}

			_attributeInfo = new AttributeInfo(name, prefix, prefixOffset, suffix, suffixOffset, attributeValuesCount);

			// Single valued attributes might be omitted in entirety if it the attribute value strictly evaluates to
			// null  or false. Consequently defer the prefix generation until we encounter the attribute value.
			if (attributeValuesCount != 1)
			{
				WritePositionTaggedLiteral(prefix, prefixOffset);
			}
		}

		public void WriteAttributeValue(
			string prefix,
			int prefixOffset,
			object? value,
			int valueOffset,
			int valueLength,
			bool isLiteral)
		{
			if (_attributeInfo.AttributeValuesCount == 1)
			{
				if (IsBoolFalseOrNullValue(prefix, value))
				{
					// Value is either null or the bool 'false' with no prefix; don't render the attribute.
					_attributeInfo.Suppressed = true;
					return;
				}

				// We are not omitting the attribute. Write the prefix.
				WritePositionTaggedLiteral(_attributeInfo.Prefix, _attributeInfo.PrefixOffset);

				if (IsBoolTrueWithEmptyPrefixValue(prefix, value))
				{
					// The value is just the bool 'true', write the attribute name instead of the string 'True'.
					value = _attributeInfo.Name;
				}
			}

			// This block handles two cases.
			// 1. Single value with prefix.
			// 2. Multiple values with or without prefix.
			if (value != null)
			{
				if (!string.IsNullOrEmpty(prefix))
				{
					WritePositionTaggedLiteral(prefix, prefixOffset);
				}

				BeginContext(valueOffset, valueLength, isLiteral);

				WriteUnprefixedAttributeValue(value, isLiteral);

				EndContext();
			}
		}

		public virtual void EndWriteAttribute()
		{
			if (!_attributeInfo.Suppressed)
			{
				WritePositionTaggedLiteral(_attributeInfo.Suffix, _attributeInfo.SuffixOffset);
			}
		}

		private void WriteUnprefixedAttributeValue(object value, bool isLiteral)
		{
			var stringValue = value as string;

			// The extra branching here is to ensure that we call the Write*To(string) overload where possible.
			if (isLiteral && stringValue != null)
			{
				WriteLiteral(stringValue);
			}
			else if (isLiteral)
			{
				WriteLiteral(value);
			}
			else if (stringValue != null)
			{
				Write(stringValue);
			}
			else
			{
				Write(value);
			}
		}

		private void WritePositionTaggedLiteral(string value, int position)
		{
			BeginContext(position, value.Length, isLiteral: true);
			WriteLiteral(value);
			EndContext();
		}

		#endregion

		#region Helpers

		private bool IsBoolFalseOrNullValue(string prefix, object? value)
		{
			return string.IsNullOrEmpty(prefix) &&
				(value == null ||
				(value is bool && !(bool)value));
		}

		private bool IsBoolTrueWithEmptyPrefixValue(string prefix, object? value)
		{
			// If the value is just the bool 'true', use the attribute name as the value.
			return string.IsNullOrEmpty(prefix) &&
				(value is bool && (bool)value);
		}

		#endregion

		#region structs

		private struct AttributeInfo
		{
			public AttributeInfo(
				string name,
				string prefix,
				int prefixOffset,
				string suffix,
				int suffixOffset,
				int attributeValuesCount)
			{
				Name = name;
				Prefix = prefix;
				PrefixOffset = prefixOffset;
				Suffix = suffix;
				SuffixOffset = suffixOffset;
				AttributeValuesCount = attributeValuesCount;

				Suppressed = false;
			}

			public int AttributeValuesCount { get; }

			public string Name { get; }

			public string Prefix { get; }

			public int PrefixOffset { get; }

			public string Suffix { get; }

			public int SuffixOffset { get; }

			public bool Suppressed { get; set; }
		}

		#endregion
	}
}
