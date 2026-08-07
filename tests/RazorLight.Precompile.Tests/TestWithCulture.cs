using System.Globalization;

namespace RazorLight.Precompile.Tests
{
	public class TestWithCulture
	{
		public TestWithCulture()
		{
			var culture = (CultureInfo)CultureInfo.GetCultureInfo("en-US").Clone();
			culture.DateTimeFormat.ShortDatePattern = "M/d/yyyy";
			culture.DateTimeFormat.ShortTimePattern = "h:mm tt";
			culture.DateTimeFormat.LongTimePattern = "h:mm:ss tt";
			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = culture;
		}
	}
}
