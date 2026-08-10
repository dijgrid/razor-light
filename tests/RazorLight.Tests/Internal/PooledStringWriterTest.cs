using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using RazorLight.Internal.Buffering;
using Xunit;

namespace RazorLight.Tests.Internal
{
	public sealed class PooledStringWriterTest
	{
		[Fact]
		public async Task Materializes_one_result_across_segment_and_write_boundaries()
		{
			var pool = new TrackingArrayPool();
			string result;
			using (var writer = new PooledStringWriter(pool))
			{
				writer.Write(new string('a', 1_500));
				writer.Write('b');
				writer.Write(new[] { 'c', 'd', 'e' }, 1, 2);
				await writer.WriteAsync("tail".AsMemory(), CancellationToken.None);
				result = writer.ToString();
			}

			Assert.Equal(new string('a', 1_500) + "bdetail", result);
			Assert.True(pool.RentCount > 1);
			Assert.Equal(pool.RentCount, pool.ReturnCount);
			Assert.True(pool.ReturnedBuffersWereCleared);
		}

		private sealed class TrackingArrayPool : ArrayPool<char>
		{
			public int RentCount { get; private set; }
			public int ReturnCount { get; private set; }
			public bool ReturnedBuffersWereCleared { get; private set; } = true;

			public override char[] Rent(int minimumLength)
			{
				RentCount++;
				return new char[minimumLength];
			}

			public override void Return(char[] array, bool clearArray = false)
			{
				ArgumentNullException.ThrowIfNull(array);
				ReturnCount++;
				ReturnedBuffersWereCleared &= Array.TrueForAll(array, value => value == '\0');
			}
		}
	}
}
