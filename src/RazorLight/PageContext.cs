using System.Dynamic;
using System.IO;

using System.Threading;

namespace RazorLight
{
	/// <inheritdoc />
	public sealed class PageContext : IPageContext
	{
		private dynamic _viewBag;
		private readonly ExpandoObject _viewBagData;

		/// <inheritdoc />
		public PageContext()
		{
			_viewBagData = new ExpandoObject();
			_viewBag = new RazorLightViewBag(_viewBagData);
			Writer = new StringWriter();
		}

		/// <inheritdoc />
		public PageContext(ExpandoObject? viewBag)
		{
			_viewBagData = viewBag ?? new ExpandoObject();
			_viewBag = new RazorLightViewBag(_viewBagData);
			Writer = new StringWriter();
		}

		/// <inheritdoc />
		public TextWriter Writer { get; set; }

		/// <inheritdoc />
		public CancellationToken CancellationToken { get; set; }

		/// <inheritdoc />
		public dynamic ViewBag => _viewBag;
		internal ExpandoObject ViewBagData => _viewBagData;

		/// <inheritdoc />
		public string? ExecutingPageKey { get; set; }

		/// <inheritdoc />
		public ModelTypeInfo? ModelTypeInfo { get; set; }

		/// <inheritdoc />
		public object? Model { get; set; }
	}
}
