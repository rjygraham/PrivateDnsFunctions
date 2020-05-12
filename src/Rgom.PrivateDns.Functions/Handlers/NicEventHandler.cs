using Microsoft.Azure.Management.PrivateDns.Models;
using Rgom.PrivateDns.Functions.Data;
using Rgom.PrivateDns.Functions.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rgom.PrivateDns.Functions.Handlers
{
	internal class NicEventHandler : INicEventHandler
	{
		private readonly INetworkManagementService networkManagementService;
		private readonly IPrivateDnsManagementService privateDnsManagementService;
		private readonly IDnsEntityService dnsEntityService;

		private readonly string defaultPrivateDnsZone;
		private readonly string hostNameTagName;

		public NicEventHandler(INetworkManagementService networkManagementService, IPrivateDnsManagementService privateDnsManagementService, IDnsEntityService dnsEntityService, string defaultPrivateDnsZone, string hostNameTagName)
		{
			this.networkManagementService = networkManagementService ?? throw new ArgumentNullException(nameof(networkManagementService));
			this.privateDnsManagementService = privateDnsManagementService ?? throw new ArgumentNullException(nameof(privateDnsManagementService));
			this.dnsEntityService = dnsEntityService ?? throw new ArgumentNullException(nameof(dnsEntityService));

			this.defaultPrivateDnsZone = defaultPrivateDnsZone ?? throw new ArgumentNullException(nameof(defaultPrivateDnsZone));
			this.hostNameTagName = hostNameTagName ?? throw new ArgumentNullException(nameof(hostNameTagName));
		}
		
		public async Task<bool> HandleNicCreatedEventAsync(string subscriptionId, string resourceId)
		{
			bool result;

			var resourceGroupName = Constants.ResourceGroupCaptureRegEx.Match(resourceId).Groups["resourcegroup"].Value;
			var networkInterfaceName = Constants.NicCaptureRegEx.Match(resourceId).Groups["nic"].Value;

			// Get NIC that was just created.
			networkManagementService.SetSubscriptionId(subscriptionId);
			var nic = await networkManagementService.GetNetworkInterfaceAsync(resourceGroupName, networkInterfaceName);

			var ipConfig = nic.IpConfigurations[0];

			// Ignore if this is a private endpoint NIC, 
			if (ipConfig.Name.Contains("privateEndpoint", StringComparison.InvariantCultureIgnoreCase))
			{
				return true;
			}

			var hostname = nic.Tags[hostNameTagName];

			// If NIC wasn't tagged there's nothing for us to do so just return.
			if (string.IsNullOrEmpty(hostname))
			{
				return true;
			}

			try
			{
				// Create new recordset in appropriate private DNS zone.
				var newRecordSet = new RecordSet(aRecords: new List<ARecord> { new ARecord(ipConfig.PrivateIPAddress) }, ttl: 3600);
				var dnsResult = await privateDnsManagementService.CreateOrUpdateAsync(defaultPrivateDnsZone, RecordType.A, hostname, newRecordSet).ConfigureAwait(false);

				// Save the record as Table Entity so we can delete.
				var dnsEntity = new DnsEntity(resourceId, hostname, defaultPrivateDnsZone, RecordType.A, ipConfig.PrivateIPAddress);
				await dnsEntityService.InsertOrUpdateDnsEntityAsync(dnsEntity).ConfigureAwait(false);

				return true;
			}
			catch (Exception ex)
			{
				throw;
			}
			
			return false;
		}

		public async Task<bool> HandleNicDeletedEventAsync(string subscriptionId, string resourceId)
		{
			foreach (var dnsEntity in await dnsEntityService.ListDnsEntitiesAsync(resourceId))
			{
				await privateDnsManagementService.DeleteAsync(dnsEntity.DnsZone, dnsEntity.RecordType, dnsEntity.RowKey);
				await dnsEntityService.DeleteDnsEntityAsync(dnsEntity);
			}

			return true;
		}

	}
}
