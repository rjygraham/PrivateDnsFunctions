using System.Collections.Generic;
using System.Linq;

namespace Rgom.PrivateDns.Functions.Extensions
{
	public static class PrivateLinkServiceIdExtensions
	{
		public static HashSet<string> ToPrivateDnsZoneLookupKeys(this string privateLinkServiceId, IList<string> groupIds)
		{
			var result = new HashSet<string>();

			var resourceIdParts = privateLinkServiceId.Split('/');

			foreach (var groupId in groupIds)
			{
				result.Add($"{resourceIdParts[6]}/{resourceIdParts[7]}/{groupId}");
			}

			return result;
		}
	}
}
