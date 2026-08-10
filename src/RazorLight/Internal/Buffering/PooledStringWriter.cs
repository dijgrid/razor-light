using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RazorLight.Internal.Buffering
{
	/// <summary>
	/// Accumulates characters in pooled segments and materializes one final string without retaining
	/// the intermediate contiguous buffer used by <see cref="StringWriter"/>.
	/// </summary>
	internal sealed class PooledStringWriter : TextWriter
	{
		private readonly ArrayPool<char> _pool;
		private char[]? _buffer;
		private int _length;

		public PooledStringWriter() : this(ArrayPool<char>.Shared)
		{
		}

		internal PooledStringWriter(ArrayPool<char> pool)
		{
			_pool = pool ?? throw new ArgumentNullException(nameof(pool));
		}

		public override Encoding Encoding => Encoding.Unicode;

		public override void Write(char value)
		{
			EnsureCapacity(checked(_length + 1));
			_buffer![_length++] = value;
		}

		public override void Write(char[]? buffer)
		{
			if (buffer != null) Write(buffer.AsSpan());
		}

		public override void Write(char[] buffer, int index, int count)
		{
			ArgumentNullException.ThrowIfNull(buffer);
			if (index < 0 || index > buffer.Length) throw new ArgumentOutOfRangeException(nameof(index));
			if (count < 0 || buffer.Length - index < count) throw new ArgumentOutOfRangeException(nameof(count));
			Write(buffer.AsSpan(index, count));
		}

		public override void Write(string? value)
		{
			if (value != null) Write(value.AsSpan());
		}

		public override void Write(ReadOnlySpan<char> buffer)
		{
			if (buffer.IsEmpty) return;
			EnsureCapacity(checked(_length + buffer.Length));
			buffer.CopyTo(_buffer.AsSpan(_length));
			_length += buffer.Length;
		}

		public override Task WriteAsync(char value)
		{
			Write(value);
			return Task.CompletedTask;
		}

		public override Task WriteAsync(char[] buffer, int index, int count)
		{
			Write(buffer, index, count);
			return Task.CompletedTask;
		}

		public override Task WriteAsync(string? value)
		{
			Write(value);
			return Task.CompletedTask;
		}

		public override Task WriteAsync(
			ReadOnlyMemory<char> buffer,
			CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Write(buffer.Span);
			return Task.CompletedTask;
		}

		public override string ToString() => _length == 0
			? string.Empty
			: new string(_buffer!, 0, _length);

		private void EnsureCapacity(int requiredCapacity)
		{
			if (_buffer != null && requiredCapacity <= _buffer.Length) return;

			int requestedCapacity = _buffer == null
				? requiredCapacity
				: Math.Max(requiredCapacity, _buffer.Length <= int.MaxValue / 2
					? _buffer.Length * 2
					: requiredCapacity);
			char[] replacement = _pool.Rent(requestedCapacity);
			if (_buffer != null)
			{
				_buffer.AsSpan(0, _length).CopyTo(replacement);
				ReturnBuffer(_buffer, _length);
			}

			_buffer = replacement;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && _buffer != null)
			{
				ReturnBuffer(_buffer, _length);
				_buffer = null;
				_length = 0;
			}
			base.Dispose(disposing);
		}

		private void ReturnBuffer(char[] buffer, int usedLength)
		{
			Array.Clear(buffer, 0, usedLength);
			_pool.Return(buffer);
		}
	}
}
