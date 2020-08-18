using System;
using System.Collections.Generic;
using System.Text;

namespace Rgom.PrivateDns.Functions.Models
{
	public struct DeployPrivateDnsZoneGroupsArmTemplateParameters
	{
		public string SubscriptionId { get; set; }
		public string PrivateEndpointResourceId { get; set; }
		public string PrivateDnsZone { get; set; }
	}
}
