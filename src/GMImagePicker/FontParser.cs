using System;
using System.Linq;
using UIKit;

namespace GMImagePicker
{
	internal static class FontParser
	{
		public static UIFont GetFont(string family, nfloat size)
		{
			UIFont result;

			if (family.StartsWith(".SFUI", System.StringComparison.InvariantCultureIgnoreCase))
			{
				var fontWeight = family.Split('-').LastOrDefault();

				if (!string.IsNullOrWhiteSpace(fontWeight) && System.Enum.TryParse<UIFontWeight>(fontWeight, true, out var uIFontWeight))
				{
					result = UIFont.SystemFontOfSize(size, uIFontWeight);
					return result;
				}

				result = UIFont.SystemFontOfSize(size, UIFontWeight.Regular);
				return result;
			}
			else
			{
				result = GetFont(family, size);
				if (result != null)
					return result;
			}

			// fallback
			return UIFont.SystemFontOfSize(size);
		}
	}
}
