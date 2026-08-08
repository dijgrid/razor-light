using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using RazorLight.Text;

namespace RazorLight.Internal.Buffering
{
	/// <summary>
	/// A text writer backed by a pooled template buffer and, optionally, a final writer.
	/// </summary>
	public class ViewBufferTextWriter : TextWriter
	{
		private readonly TextWriter? _inner;

		public ViewBufferTextWriter(ViewBuffer buffer, Encoding encoding)
		{
			Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
			Encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
		}

		public ViewBufferTextWriter(ViewBuffer buffer, Encoding encoding, TextWriter inner)
			: this(buffer, encoding)
		{
			_inner = inner ?? throw new ArgumentNullException(nameof(inner));
		}

		public override Encoding Encoding { get; }

		public bool IsBuffering { get; private set; } = true;

		public ViewBuffer Buffer { get; }

		public override void Write(char value) => Write(value.ToString());

		public override void Write(char[] buffer, int index, int count)
		{
			if (buffer == null) throw new ArgumentNullException(nameof(buffer));
			if (index < 0 || index > buffer.Length) throw new ArgumentOutOfRangeException(nameof(index));
			if (count < 0 || buffer.Length - index < count) throw new ArgumentOutOfRangeException(nameof(count));

			Write(new string(buffer, index, count));
		}

		public override void Write(string? value)
		{
			if (string.IsNullOrEmpty(value)) return;

			if (IsBuffering) Buffer.Append(value);
			else _inner!.Write(value);
		}

		public override void Write(object? value)
		{
			if (value is ITemplateContent content) Write(content);
			else Write(value?.ToString());
		}

		public void Write(ITemplateContent? value)
		{
			if (value == null) return;

			if (IsBuffering)
			{
				if (value is ViewBuffer buffer) buffer.MoveTo(Buffer);
				else Buffer.Append(value);
			}
			else
			{
				value.WriteTo(_inner!);
			}
		}

		public override void WriteLine() => Write(NewLine);

		public override void WriteLine(string? value)
		{
			Write(value);
			Write(NewLine);
		}

		public override void WriteLine(object? value)
		{
			Write(value);
			Write(NewLine);
		}

		public override Task WriteAsync(char value)
		{
			if (IsBuffering)
			{
				Write(value);
				return Task.CompletedTask;
			}

			return _inner!.WriteAsync(value);
		}

		public override Task WriteAsync(char[] buffer, int index, int count)
		{
			if (IsBuffering)
			{
				Write(buffer, index, count);
				return Task.CompletedTask;
			}

			return _inner!.WriteAsync(buffer, index, count);
		}

		public override Task WriteAsync(string? value)
		{
			if (IsBuffering)
			{
				Write(value);
				return Task.CompletedTask;
			}

			return _inner!.WriteAsync(value);
		}

		public override Task WriteLineAsync()
		{
			if (IsBuffering)
			{
				WriteLine();
				return Task.CompletedTask;
			}

			return _inner!.WriteLineAsync();
		}

		public override Task WriteLineAsync(char value)
		{
			if (IsBuffering)
			{
				WriteLine(value);
				return Task.CompletedTask;
			}

			return _inner!.WriteLineAsync(value);
		}

		public override Task WriteLineAsync(char[]? buffer, int index, int count)
		{
			if (buffer == null) return WriteLineAsync();
			if (IsBuffering)
			{
				Write(buffer, index, count);
				WriteLine();
				return Task.CompletedTask;
			}

			return _inner!.WriteLineAsync(buffer, index, count);
		}

		public override Task WriteLineAsync(string? value)
		{
			if (IsBuffering)
			{
				WriteLine(value);
				return Task.CompletedTask;
			}

			return _inner!.WriteLineAsync(value);
		}

		public override void Flush()
		{
			if (_inner == null || _inner is ViewBufferTextWriter) return;

			if (IsBuffering)
			{
				IsBuffering = false;
				Buffer.WriteTo(_inner);
				Buffer.Clear();
			}

			_inner.Flush();
		}

		public override async Task FlushAsync()
		{
			if (_inner == null || _inner is ViewBufferTextWriter) return;

			if (IsBuffering)
			{
				IsBuffering = false;
				await Buffer.WriteToAsync(_inner).ConfigureAwait(false);
				Buffer.Clear();
			}

			await _inner.FlushAsync().ConfigureAwait(false);
		}
	}
}
