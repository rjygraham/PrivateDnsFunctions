using Microsoft.Azure.Management.PrivateDns.Models;
using Newtonsoft.Json;
using Rgom.PrivateDns.Functions.Data;
using Rgom.PrivateDns.Functions.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rgom.PrivateDns.Functions.Handlers
{
	internal class PrivateEndpointEventHandler : IPrivateEndpointEventHandler
	{
		/// <summary>
		/// Still missing:
		/// 
		/// privatelink.postgres.database.azure.com
		/// {guid}.privatelink..azmk8s.io
		/// privatelink.search.windows.net
		/// privatelink.azurecr.io
		/// privatelink.{region}.backup.windowsazure.com
		/// topic.{region}.privatelink.eventgrid.azure.net
		/// domain.{region}.privatelink.eventgrid.azure.net
		/// </summary>
		private readonly Dictionary<string, string> zoneMapping = JsonConvert.DeserializeObject<Dictionary<string, string>>(@"
		{
			""database.windows.net"": ""privatelink.database.windows.net"",
			""blob.core.windows.net"": ""privatelink.blob.core.windows.net"",
			""table.core.windows.net"": ""privatelink.table.core.windows.net"",
			""queue.core.windows.net"": ""privatelink.queue.core.windows.net"",
			""file.core.windows.net"": ""privatelink.file.core.windows.net"",
			""web.core.windows.net"": ""privatelink.web.core.windows.net"",
			""dfs.core.windows.net"": ""privatelink.dfs.core.windows.net"",
			""documents.azure.com"": ""privatelink.documents.azure.com"",
			""mongo.cosmos.azure.com"": ""privatelink.mongo.cosmos.azure.com"",
			""cassandra.cosmos.azure.com"": ""privatelink.cassandra.cosmos.azure.com"",
			""gremlin.cosmos.azure.com"": ""privatelink.gremlin.cosmos.azure.com"",
			""table.cosmos.azure.com"": ""privatelink.table.cosmos.azure.com"",
			""mysql.database.azure.com"": ""privatelink.mysql.database.azure.com"",
			""mariadb.database.azure.com"": ""privatelink.mariadb.database.azure.com"",
			""vault.azure.net"": ""privatelink.vaultcore.azure.net"",
			""privatelink.azconfig.io"": ""privatelink.azconfig.io"",
			""servicebus.windows.net"": ""privatelink.servicebus.windows.net"",
			""azurewebsites.net"": ""privatelink.azurewebsites.net"",
			""api.azureml.ms"": ""privatelink.api.azureml.ms""
		}");

		private readonly INetworkManagementService networkManagementService;
		private readonly IPrivateDnsManagementService privateDnsManagementService;
		private readonly IDnsEntityService dnsEntityService;

		public PrivateEndpointEventHandler(INetworkManagementService networkManagementService, IPrivateDnsManagementService privateDnsManagementService, IDnsEntityService dnsEntityService)
		{
			this.networkManagementService = networkManagementService;
			this.privateDnsManagementService = privateDnsManagementService;
			this.dnsEntityService = dnsEntityService;
		}

		public async Task<bool> HandlePrivateEndpointCreatedEventAsync(string subscriptionId, string resourceId)
		{
			var resourceGroupName = Constants.ResourceGroupCaptureRegEx.Match(resourceId).Groups["resourcegroup"].Value;
			var privateEndpointName = Constants.PrivateEndpointCaptureRegEx.Match(resourceId).Groups["privateEndpoint"].Value;

			bool result = true;

			// Get Private Endpoint that was just created.
			networkManagementService.SetSubscriptionId(subscriptionId);
			var privateEndpoint = await networkManagementService.GetPrivateEndpointAsync(resourceGroupName, privateEndpointName);

			// PrivateEndpoints may have more than one DNS config - enumerate them all.
			foreach (var customDnsConfig in privateEndpoint.CustomDnsConfigs)
			{
				var hostname = customDnsConfig.Fqdn.Substring(0, customDnsConfig.Fqdn.IndexOf('.'));
				var privateDnsZone = zoneMapping.Single(s => customDnsConfig.Fqdn.Contains(s.Key)).Value;

				// Create new recordset in appropriate private DNS zone.
				var newRecordSet = new RecordSet(aRecords: new List<ARecord> { new ARecord(customDnsConfig.IpAddresses[0]) }, ttl: 3600);
				var savedRecordSet = await privateDnsManagementService.CreateOrUpdateAsync(privateDnsZone, RecordType.A, hostname, newRecordSet);

				// Save the record as Table Entity so we can delete.
				var dnsEntity = new DnsEntity(privateEndpoint.Id, hostname, privateDnsZone, RecordType.A, customDnsConfig.IpAddresses[0]);
				await dnsEntityService.InsertOrUpdateDnsEntityAsync(dnsEntity);
			}

			return result;
		}

		public async Task<bool> HandlePrivateEndpointDeletedEventAsync(string subscriptionId, string resourceId)
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
