using Microsoft.Azure.Management.PrivateDns.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rgom.PrivateDns.Functions.Models
{
	public struct DnsParameters
	{
		public string ResourceId { get; set; }
		public string DnsZone { get; set; }
		public RecordType RecordType { get; set; }
		public string Hostname { get; set; }
		public string IpAddress { get; set; }
	}
}
