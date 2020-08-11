using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.Management.Network.Models;
using Microsoft.Azure.Management.PrivateDns.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rgom.PrivateDns.Functions.Data;
using Rgom.PrivateDns.Functions.Models;
using Rgom.PrivateDns.Functions.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Rgom.PrivateDns.Functions
{
	public class PrivateEndpointEventFunctions
	{

		/// <summary>
		/// Still missing:
		/// 
		/// privatelink.{region}.azmk8s.io
		/// privatelink.{region}.backup.windowsazure.com
		/// </summary>
		private readonly Dictionary<string, string> zoneMapping = JsonConvert.DeserializeObject<Dictionary<string, string>>(@"
		{
			""azure-automation.net"": ""privatelink.azure-automation.net"",
			""database.windows.net"": ""privatelink.database.windows.net"",
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
			""postgres.database.azure.com"": ""privatelink.postgres.database.azure.com"",
			""mysql.database.azure.com"": ""privatelink.mysql.database.azure.com"",
			""mariadb.database.azure.com"": ""privatelink.mariadb.database.azure.com"",
			""vault.azure.net"": ""privatelink.vaultcore.azure.net"",
			""vaultcore.azure.net"": ""privatelink.vaultcore.azure.net"",
			""search.windows.net"": ""privatelink.search.windows.net"",
			""azurecr.io"": ""privatelink.azurecr.io"",
			""azconfig.io"": ""privatelink.azconfig.io"",
			""servicebus.windows.net"": ""privatelink.servicebus.windows.net"",
			""servicebus.windows.net"": ""privatelink.servicebus.windows.net"",
			""azure-devices.net"": ""privatelink.azure-devices.net"",
			""servicebus.windows.net"": ""privatelink.servicebus.windows.net"",
			""eventgrid.azure.net"": ""privatelink.eventgrid.azure.net"",
			""eventgrid.azure.net"": ""privatelink.eventgrid.azure.net"",
			""azurewebsites.net"": ""privatelink.azurewebsites.net"",
			""api.azureml.ms"": ""privatelink.api.azureml.ms"",
			""azure-devices.net"": ""privatelink.azure-devices.net"",
			""service.signalr.net"": ""privatelink.service.signalr.net"",
			""monitor.azure.com"": ""privatelink.monitor.azure.com"",
			""oms.opinsights.azure.com"": ""privatelink.oms.opinsights.azure.com"",
			""ods.opinsights.azure.com"": ""privatelink.ods.opinsights.azure.com"",
			""agentsvc.azure-automation.com"": ""privatelink.agentsvc.azure-automation.com"",
			""cognitiveservices.azure.com"": ""privatelink.cognitiveservices.azure.com"",
			""afs.azure.net"": ""privatelink.afs.azure.net""
		}");

		private readonly INetworkManagementService networkManagementService;

		public PrivateEndpointEventFunctions(INetworkManagementService networkManagementService)
		{
			this.networkManagementService = networkManagementService;
		}

		[FunctionName(nameof(HandlePrivateEndpointEventsAsync))]
		public async Task HandlePrivateEndpointEventsAsync(
			[EventGridTrigger]EventGridEvent eventGridEvent,
			[DurableClient] IDurableOrchestrationClient starter,
			ILogger log
		)
		{
			string eventType = eventGridEvent.EventType;
			dynamic data = eventGridEvent.Data;
			string subscriptionId = data.subscriptionId;
			string resourceId = eventGridEvent.Subject;

			var durableParameters = new OrchestratorParameters
			{
				SubscriptionId = subscriptionId,
				ResourceId = resourceId
			};

			string instanceId;

			switch (eventType)
			{
				case "Microsoft.Resources.ResourceWriteSuccess":
					instanceId = await starter.StartNewAsync(nameof(OrchestratePrivateEndpointCreatedAsync), eventGridEvent.Id, durableParameters);
					break;
				case "Microsoft.Resources.ResourceDeleteSuccess":
					instanceId = await starter.StartNewAsync(nameof(OrchestratePrivateEndpointDeletedAsync), eventGridEvent.Id, durableParameters);
					break;
				default:
					throw new Exception();
			}

			log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
		}

		[FunctionName(nameof(OrchestratePrivateEndpointCreatedAsync))]
		public async Task<bool> OrchestratePrivateEndpointCreatedAsync([OrchestrationTrigger] IDurableOrchestrationContext context)
		{
			var orchestratorParameters = context.GetInput<OrchestratorParameters>();

			// Get Private Endpoint that was just created.
			var privateEndpoint = await context.CallActivityAsync<PrivateEndpoint>(nameof(GetPrivateEndpointAsync), orchestratorParameters);

			// PrivateEndpoints may have more than one DNS config - enumerate them all. 
			foreach (var customDnsConfig in privateEndpoint.CustomDnsConfigs)
			{
				var hostname = customDnsConfig.Fqdn.Substring(0, customDnsConfig.Fqdn.IndexOf('.'));
				var privateDnsZone = zoneMapping.Single(s => customDnsConfig.Fqdn.Contains(s.Key)).Value;

				// Create new recordset in appropriate private DNS zone.
				var dnsParameters = new DnsParameters
				{
					ResourceId = orchestratorParameters.ResourceId,
					DnsZone = privateDnsZone,
					Hostname = hostname,
					RecordType = RecordType.A,
					IpAddress = customDnsConfig.IpAddresses[0]
				};

				if (!await context.CallActivityAsync<bool>(nameof(SharedDurableFunctions.CreateDnsRecordSetAsync), dnsParameters))
				{
					return false;
				}

				if (!await context.CallActivityAsync<bool>(nameof(SharedDurableFunctions.CreateDnsEntityAsync), dnsParameters))
				{
					return false;
				}
			}

			return true;
		}

		[FunctionName(nameof(OrchestratePrivateEndpointDeletedAsync))]
		public async Task<bool> OrchestratePrivateEndpointDeletedAsync([OrchestrationTrigger] IDurableOrchestrationContext context)
		{
			var orchestratorParameters = context.GetInput<OrchestratorParameters>();

			// Get DNS Entities associated with this Resource Id
			var dnsEntities = await context.CallActivityAsync<List<DnsEntity>>(nameof(SharedDurableFunctions.ListDnsEntitiesAsync), orchestratorParameters.ResourceId);

			foreach (var dnsEntity in dnsEntities)
			{
				var dnsParameters = new DnsParameters
				{
					ResourceId = orchestratorParameters.ResourceId,
					DnsZone = dnsEntity.DnsZone,
					Hostname = dnsEntity.RowKey,
					IpAddress = dnsEntity.IpAddress,
					RecordType = dnsEntity.RecordType
				};

				if (!await context.CallActivityAsync<bool>(nameof(SharedDurableFunctions.DeleteDnsRecordSetAsync), dnsParameters))
				{
					return false;
				}

				if (!await context.CallActivityAsync<bool>(nameof(SharedDurableFunctions.DeleteDnsEntityAsync), dnsEntity))
				{
					return false;
				}
			}

			return true;
		}

		[FunctionName(nameof(GetPrivateEndpointAsync))]
		public async Task<PrivateEndpoint> GetPrivateEndpointAsync([ActivityTrigger] OrchestratorParameters parameters, ILogger log)
		{
			var resourceGroupName = Constants.ResourceGroupCaptureRegEx.Match(parameters.ResourceId).Groups["resourcegroup"].Value;
			var privateEndpointName = Constants.PrivateEndpointCaptureRegEx.Match(parameters.ResourceId).Groups["privateEndpoint"].Value;

			networkManagementService.SetSubscriptionId(parameters.SubscriptionId);
			return await networkManagementService.GetPrivateEndpointAsync(resourceGroupName, privateEndpointName);
		}

	}
}
