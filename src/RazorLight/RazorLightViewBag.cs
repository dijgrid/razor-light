using System;
using System.Collections.Generic;
using System.Dynamic;

namespace RazorLight
{
	internal sealed class RazorLightViewBag : DynamicObject
	{
		private readonly IDictionary<string, object?> _values;

		public RazorLightViewBag(ExpandoObject values)
		{
			_values = values ?? throw new ArgumentNullException(nameof(values));
		}

		public override bool TryGetMember(GetMemberBinder binder, out object? result)
		{
			_values.TryGetValue(binder.Name, out result);
			return true;
		}

		public override bool TrySetMember(SetMemberBinder binder, object? value)
		{
			_values[binder.Name] = value;
			return true;
		}
	}
}
