using System.Text.RegularExpressions;

namespace Rgom.PrivateDns.Functions
{
	internal static class Constants
	{
		internal const string DnsEntitiesTableName = "DnsEntities";
		
		internal static Regex IPConfigCaptureRegEx = new Regex("(?<ipconfig>[a-z]+)-[\\w.]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
		internal static Regex NicCaptureRegEx = new Regex("/networkInterfaces/(?<nic>[a-z0-9-.]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
		internal static Regex PrivateEndpointCaptureRegEx = new Regex("/privateEndpoints/(?<privateEndpoint>[a-z0-9-.]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
		internal static Regex ResourceGroupCaptureRegEx = new Regex("/resourcegroups/(?<resourcegroup>[a-z0-9-]+)/", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
	}
}
