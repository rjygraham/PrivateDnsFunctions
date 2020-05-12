using Microsoft.Azure.Management.PrivateDns.Models;
using Microsoft.WindowsAzure.Storage.Table;

namespace Rgom.PrivateDns.Functions.Data
{
	public class DnsEntity : TableEntity
	{
		public string DnsZone { get; set; }
		public RecordType RecordType { get; set; }
		public string IpAddress { get; set; }

		public DnsEntity()
		{
		}

		public DnsEntity(string resourceId, string hostname)
		{
			PartitionKey = resourceId.Replace('/', ':').ToLower();
			RowKey = hostname.ToLower();
		}

		public DnsEntity(string resourceId, string hostname, string dnsZone, RecordType recordType, string ipAddress)
			: this(resourceId, hostname)
		{
			DnsZone = dnsZone;
			RecordType = recordType;
			IpAddress = ipAddress;
		}
	}
}
