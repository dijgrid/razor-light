namespace RazorLight.Internal.Buffering
{
	internal interface ICharBufferSource
	{
		char[] Rent(int bufferSize);

		void Return(char[] buffer);
	}
}
