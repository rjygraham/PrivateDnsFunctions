using System;
using System.Collections.Generic;
using System.Text;

namespace Rgom.PrivateDns.Functions.Extensions
{
	public static class StringExtensions
	{
		public static int IndexOfNth(this string input, char value, int nth)
		{
			if (string.IsNullOrWhiteSpace(input))
			{
				return -1;
			}

			var currentIndex = -1;
			var currentCount = 0;

			while (currentCount < nth)
			{
				currentIndex++;
				currentIndex = input.IndexOf(value, currentIndex);
				if (currentIndex == -1)
				{
					return -1;
				}
				currentCount++;
			}

			return currentIndex;
		}
	}
}
