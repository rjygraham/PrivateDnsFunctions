using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.Management.Network.Models;
using Microsoft.Azure.Management.PrivateDns.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Rgom.PrivateDns.Functions.Data;
using Rgom.PrivateDns.Functions.Models;
using Rgom.PrivateDns.Functions.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rgom.PrivateDns.Functions
{
	public class NetworkInterfaceEventFunctions
	{
		private readonly INetworkManagementService networkManagementService;
		private readonly string defaultPrivateDnsZone;
		private readonly string hostNameTagName;

		public NetworkInterfaceEventFunctions(INetworkManagementService networkManagementService, IConfiguration configuration)
		{
			this.networkManagementService = networkManagementService ?? throw new ArgumentNullException(nameof(networkManagementService));
			this.defaultPrivateDnsZone = configuration.GetValue<string>("DefaultPrivateDnsZone");
			this.hostNameTagName = configuration.GetValue<string>("HostnameTagName");
		}

		[FunctionName(nameof(HandleNetworkInterfaceEventsAsync))]
		public async Task HandleNetworkInterfaceEventsAsync(
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
					instanceId = await starter.StartNewAsync(nameof(OrchestrateNetworkInterfaceCreatedAsync), eventGridEvent.Id, durableParameters);
					break;
				case "Microsoft.Resources.ResourceDeleteSuccess":
					instanceId = await starter.StartNewAsync(nameof(OrchestrateNetworkInterfaceDeletedAsync), eventGridEvent.Id, durableParameters);
					break;
				default:
					throw new Exception();
			}

			log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
		}

		[FunctionName(nameof(OrchestrateNetworkInterfaceCreatedAsync))]
		public async Task<bool> OrchestrateNetworkInterfaceCreatedAsync([OrchestrationTrigger] IDurableOrchestrationContext context)
		{
			var orchestratorParameters = context.GetInput<OrchestratorParameters>();

			// Get NIC that was just created.
			var nic = await context.CallActivityAsync<NetworkInterface>(nameof(GetNetworkInterfaceAsync), orchestratorParameters);

			var ipConfig = nic.IpConfigurations[0];

			// Ignore if this is a private endpoint NIC - this is handled by the PrivateEndpointEventFunctions.
			if (ipConfig.Name.Contains("privateEndpoint", StringComparison.InvariantCultureIgnoreCase))
			{
				return true;
			}

			string hostname = null;

			// If NIC wasn't tagged there's nothing for us to do so just return.
			if (nic.Tags == null || !nic.Tags.TryGetValue(hostNameTagName, out hostname))
			{
				return true;
			}

			// Create new recordset in default private DNS zone.
			var dnsParameters = new DnsParameters
			{
				ResourceId = orchestratorParameters.ResourceId,
				DnsZone = defaultPrivateDnsZone,
				Hostname = hostname,
				RecordType = RecordType.A,
				IpAddress = ipConfig.PrivateIPAddress
			};

			var recordSetCreated = await context.CallActivityAsync<bool>(nameof(SharedDurableFunctions.CreateDnsRecordSetAsync), dnsParameters);

			if (recordSetCreated)
			{
				// Save the record as Table Entity so we can delete.
				return await context.CallActivityAsync<bool>(nameof(SharedDurableFunctions.CreateDnsEntityAsync), dnsParameters);
			}

			return false;
		}

		[FunctionName(nameof(OrchestrateNetworkInterfaceDeletedAsync))]
		public async Task<bool> OrchestrateNetworkInterfaceDeletedAsync([OrchestrationTrigger] IDurableOrchestrationContext context)
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

				if (await context.CallActivityAsync<bool>(nameof(SharedDurableFunctions.DeleteDnsRecordSetAsync), dnsParameters))
				{
					return await context.CallActivityAsync<bool>(nameof(SharedDurableFunctions.DeleteDnsEntityAsync), dnsEntity);
				}
			}

			return false;
		}

		[FunctionName(nameof(GetNetworkInterfaceAsync))]
		public async Task<NetworkInterface> GetNetworkInterfaceAsync([ActivityTrigger] OrchestratorParameters parameters, ILogger log)
		{
			var resourceGroupName = Constants.ResourceGroupCaptureRegEx.Match(parameters.ResourceId).Groups["resourcegroup"].Value;
			var networkInterfaceName = Constants.NicCaptureRegEx.Match(parameters.ResourceId).Groups["nic"].Value;

			networkManagementService.SetSubscriptionId(parameters.SubscriptionId);
			return await networkManagementService.GetNetworkInterfaceAsync(resourceGroupName, networkInterfaceName);
		}
	}
}
