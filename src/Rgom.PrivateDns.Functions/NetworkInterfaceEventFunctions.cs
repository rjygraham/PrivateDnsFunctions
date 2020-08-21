using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.Management.Network.Models;
using Microsoft.Azure.Management.PrivateDns.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Rgom.PrivateDns.Functions.Data;
using Rgom.PrivateDns.Functions.Models;
using Rgom.PrivateDns.Functions.Services;
using System;
using System.Linq;
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
					instanceId = await starter.StartNewAsync(nameof(OrchestrateNetworkInterfaceWriteAsync), eventGridEvent.Id, durableParameters);
					break;
				case "Microsoft.Resources.ResourceDeleteSuccess":
					instanceId = await starter.StartNewAsync(nameof(OrchestrateNetworkInterfaceDeleteAsync), eventGridEvent.Id, durableParameters);
					break;
				default:
					throw new Exception();
			}

			log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
		}

		[FunctionName(nameof(OrchestrateNetworkInterfaceWriteAsync))]
		public async Task<bool> OrchestrateNetworkInterfaceWriteAsync([OrchestrationTrigger] IDurableOrchestrationContext context)
		{
			var orchestratorParameters = context.GetInput<OrchestratorParameters>();

			// Get NIC that was just created.
			var nic = await context.CallActivityAsync<NetworkInterface>(nameof(GetNetworkInterfaceAsync), orchestratorParameters);

			// Ignore if this is a private endpoint NIC - this is handled by the PrivateEndpointEventFunctions.
			var ipConfig = nic.IpConfigurations[0];
			if (ipConfig.Name.Contains("privateEndpoint", StringComparison.InvariantCultureIgnoreCase))
			{
				return true;
			}

			var dnsEntity = await context.CallActivityAsync<DnsEntity>(nameof(SharedDurableFunctions.GetDnsEntityAsync), orchestratorParameters.ResourceId);

			if (dnsEntity == null)
			{
				return await OrchestrateNetworkInterfaceCreateAsync(context, nic);
			}
			else
			{
				return await OrchestrateNetworkInterfaceUpdateAsync(context, nic, dnsEntity);
			}
		}

		private async Task<bool> OrchestrateNetworkInterfaceCreateAsync(IDurableOrchestrationContext context, NetworkInterface nic)
		{
			// If NIC wasn't tagged [with a value] there's nothing for us to do so just return.
			if (nic.Tags == null || !nic.Tags.ContainsKey(hostNameTagName))
			{
				return true;
			}

			// If NIC tag is empty string there's nothing for us to do so just return.
			var hostname = nic.Tags.SingleOrDefault(s => s.Key.Equals(hostNameTagName, StringComparison.OrdinalIgnoreCase)).Value;
			if (string.IsNullOrWhiteSpace(hostname))
			{
				return true;
			}

			// Create new recordset in default private DNS zone.
			var dnsParameters = new DnsParameters
			{
				ResourceId = nic.Id,
				DnsZone = defaultPrivateDnsZone,
				Hostname = hostname,
				RecordType = RecordType.A,
				IpAddress = nic.IpConfigurations[0].PrivateIPAddress
			};

			var recordSetCreated = await context.CallActivityAsync<bool>(nameof(SharedDurableFunctions.CreateDnsRecordSetAsync), dnsParameters);

			if (recordSetCreated)
			{
				// Save the record as Table Entity so we can delete.
				return await context.CallActivityAsync<bool>(nameof(SharedDurableFunctions.CreateDnsEntityAsync), dnsParameters);
			}

			return false;
		}

		private async Task<bool> OrchestrateNetworkInterfaceUpdateAsync(IDurableOrchestrationContext context, NetworkInterface nic, DnsEntity dnsEntity)
		{
			// If NIC tags were removed or no longer contains the hostname tag.
			var remove = nic.Tags == null || !nic.Tags.ContainsKey(hostNameTagName);
			var replace = !remove && !nic.Tags[hostNameTagName].Equals(dnsEntity.Hostname, StringComparison.OrdinalIgnoreCase);
			var removed = false;

			var orchestratorParameters = new OrchestratorParameters
			{
				SubscriptionId = ResourceId.FromString(nic.Id).SubscriptionId,
				ResourceId = nic.Id
			};

			if (remove || replace)
			{
				removed = await context.CallSubOrchestratorAsync<bool>(nameof(OrchestrateNetworkInterfaceDeleteAsync), orchestratorParameters);
			}

			if (replace && removed)
			{
				return await context.CallSubOrchestratorAsync<bool>(nameof(OrchestrateNetworkInterfaceWriteAsync), orchestratorParameters);
			}

			return true;
		}

		[FunctionName(nameof(OrchestrateNetworkInterfaceDeleteAsync))]
		public async Task<bool> OrchestrateNetworkInterfaceDeleteAsync([OrchestrationTrigger] IDurableOrchestrationContext context)
		{
			var orchestratorParameters = context.GetInput<OrchestratorParameters>();

			// Get DNS Entities associated with this Resource Id
			var dnsEntity = await context.CallActivityAsync<DnsEntity>(nameof(SharedDurableFunctions.GetDnsEntityAsync), orchestratorParameters.ResourceId);

			if (dnsEntity != null)
			{
				var dnsParameters = new DnsParameters
				{
					ResourceId = orchestratorParameters.ResourceId,
					DnsZone = dnsEntity.DnsZone,
					Hostname = dnsEntity.Hostname,
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
