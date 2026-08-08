using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using RazorLight.Text;

namespace RazorLight.Internal.Buffering
{
	/// <summary>
	/// Buffers final template output in pooled pages.
	/// </summary>
	[DebuggerDisplay("{DebuggerToString()}")]
	public class ViewBuffer : ITemplateContent
	{
		public static readonly int PartialViewPageSize = 32;
		public static readonly int ViewPageSize = 256;

		private readonly IViewBufferScope _bufferScope;
		private readonly string? _name;
		private readonly int _pageSize;
		private ViewBufferPage? _currentPage;
		private List<ViewBufferPage>? _multiplePages;

		public ViewBuffer(IViewBufferScope bufferScope, string? name, int pageSize)
		{
			_bufferScope = bufferScope ?? throw new ArgumentNullException(nameof(bufferScope));
			if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));

			_name = name;
			_pageSize = pageSize;
		}

		public int Count => _multiplePages?.Count ?? (_currentPage == null ? 0 : 1);

		public ViewBufferPage this[int index]
		{
			get
			{
				if (_multiplePages != null) return _multiplePages[index];
				if (index == 0 && _currentPage != null) return _currentPage;
				throw new IndexOutOfRangeException();
			}
		}

		public ViewBuffer Append(string? value)
		{
			if (value != null) AppendValue(new ViewBufferValue(value));
			return this;
		}

		public ViewBuffer Append(ITemplateContent? content)
		{
			if (content != null) AppendValue(new ViewBufferValue(content));
			return this;
		}

		public ViewBuffer Clear()
		{
			_multiplePages = null;
			_currentPage = null;
			return this;
		}

		public void WriteTo(TextWriter writer)
		{
			if (writer == null) throw new ArgumentNullException(nameof(writer));

			for (var i = 0; i < Count; i++)
			{
				var page = this[i];
				for (var j = 0; j < page.Count; j++)
				{
					WriteValue(writer, page.Buffer[j].Value);
				}
			}
		}

		public async Task WriteToAsync(TextWriter writer)
		{
			if (writer == null) throw new ArgumentNullException(nameof(writer));

			for (var i = 0; i < Count; i++)
			{
				var page = this[i];
				for (var j = 0; j < page.Count; j++)
				{
					if (page.Buffer[j].Value is string value)
					{
						await writer.WriteAsync(value).ConfigureAwait(false);
					}
					else if (page.Buffer[j].Value is ViewBuffer nested)
					{
						await nested.WriteToAsync(writer).ConfigureAwait(false);
					}
					else if (page.Buffer[j].Value is ITemplateContent content)
					{
						content.WriteTo(writer);
					}
				}
			}
		}

		public void CopyTo(ViewBuffer destination)
		{
			if (destination == null) throw new ArgumentNullException(nameof(destination));

			for (var i = 0; i < Count; i++)
			{
				var page = this[i];
				for (var j = 0; j < page.Count; j++)
				{
					destination.AppendValue(page.Buffer[j]);
				}
			}
		}

		public void MoveTo(ViewBuffer destination)
		{
			if (destination == null) throw new ArgumentNullException(nameof(destination));

			for (var i = 0; i < Count; i++)
			{
				var page = this[i];
				var destinationPage = destination.Count == 0 ? null : destination[destination.Count - 1];
				var canCopy = 2 * page.Count <= page.Capacity && destinationPage != null &&
					destinationPage.Capacity - destinationPage.Count >= page.Count;

				if (canCopy)
				{
					Array.Copy(page.Buffer, 0, destinationPage!.Buffer, destinationPage.Count, page.Count);
					destinationPage.Count += page.Count;
					Array.Clear(page.Buffer, 0, page.Count);
					_bufferScope.ReturnSegment(page.Buffer);
				}
				else
				{
					destination.AddPage(page);
				}
			}

			Clear();
		}

		private void AppendValue(ViewBufferValue value) => GetCurrentPage().Append(value);

		private ViewBufferPage GetCurrentPage()
		{
			if (_currentPage == null || _currentPage.IsFull)
			{
				AddPage(new ViewBufferPage(_bufferScope.GetPage(_pageSize)));
			}

			return _currentPage!;
		}

		private void AddPage(ViewBufferPage page)
		{
			if (_multiplePages != null)
			{
				_multiplePages.Add(page);
			}
			else if (_currentPage != null)
			{
				_multiplePages = new List<ViewBufferPage>(2) { _currentPage, page };
			}

			_currentPage = page;
		}

		private static void WriteValue(TextWriter writer, object? value)
		{
			if (value is string text) writer.Write(text);
			else if (value is ITemplateContent content) content.WriteTo(writer);
		}

		private string DebuggerToString() => _name ?? string.Empty;
	}
}
