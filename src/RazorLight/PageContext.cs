using System.Dynamic;
using System.IO;

using System.Threading;

namespace RazorLight
{
	public sealed class PageContext : IPageContext
	{
		private dynamic _viewBag;
		private readonly ExpandoObject _viewBagData;

		public PageContext()
		{
			_viewBagData = new ExpandoObject();
			_viewBag = new RazorLightViewBag(_viewBagData);
			Writer = new StringWriter();
		}

		public PageContext(ExpandoObject? viewBag)
		{
			_viewBagData = viewBag ?? new ExpandoObject();
			_viewBag = new RazorLightViewBag(_viewBagData);
			Writer = new StringWriter();
		}

		public TextWriter Writer { get; set; }

		public CancellationToken CancellationToken { get; set; }

		public dynamic ViewBag => _viewBag;
		internal ExpandoObject ViewBagData => _viewBagData;

		public string? ExecutingPageKey { get; set; }

		public ModelTypeInfo? ModelTypeInfo { get; set; }

		public object? Model { get; set; }
	}
}
