using Microsoft.Azure.Management.PrivateDns.Models;
using Microsoft.WindowsAzure.Storage.Table;
using Rgom.PrivateDns.Functions.Extensions;

namespace Rgom.PrivateDns.Functions.Data
{
	public class DnsEntity : TableEntity
	{
		public string Hostname { get; set; }
		public string DnsZone { get; set; }
		public RecordType RecordType { get; set; }
		public string IpAddress { get; set; }

		public DnsEntity()
		{
		}

		public DnsEntity(string resourceId)
		{
			resourceId = resourceId.Replace('/', ':').ToLower();
			var index = resourceId.IndexOfNth(':', 5);

			PartitionKey = resourceId.Substring(0, index);
			RowKey = resourceId.Substring(++index);
		}

		public DnsEntity(string resourceId, string hostname, string dnsZone, RecordType recordType, string ipAddress)
			: this(resourceId)
		{
			Hostname = hostname;
			DnsZone = dnsZone;
			RecordType = recordType;
			IpAddress = ipAddress;
		}
	}
}
