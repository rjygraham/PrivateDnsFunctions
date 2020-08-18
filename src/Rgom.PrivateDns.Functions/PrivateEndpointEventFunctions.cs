using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.Management.Network.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Rgom.PrivateDns.Functions.Extensions;
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
		private readonly INetworkManagementService networkManagementService;
		private readonly IPrivateDnsManagementService privateDnsManagementService;
		private readonly IResourceManagementService resourceManagementService;

		public PrivateEndpointEventFunctions(INetworkManagementService networkManagementService, IPrivateDnsManagementService privateDnsManagementService, IResourceManagementService resourceManagementService)
		{
			this.networkManagementService = networkManagementService;
			this.privateDnsManagementService = privateDnsManagementService;
			this.resourceManagementService = resourceManagementService;
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
				//case "Microsoft.Resources.ResourceDeleteSuccess":
				//	instanceId = await starter.StartNewAsync(nameof(OrchestratePrivateEndpointDeletedAsync), eventGridEvent.Id, durableParameters);
				//	break;
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

			var addedDnsZones = new HashSet<string>();

			foreach (var privateLinkServiceConnection in privateEndpoint.PrivateLinkServiceConnections)
			{
				foreach( var key in privateLinkServiceConnection.PrivateLinkServiceId.ToPrivateDnsZoneLookupKeys(privateLinkServiceConnection.GroupIds))
				{
					var privateDnsZoneMapping = Constants.ZoneMapping.SingleOrDefault(s => s.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
					var privateDnsZone = string.Format(privateDnsZoneMapping.Value, privateEndpoint.Location.ToLower());

					if (!addedDnsZones.Contains(privateDnsZone))
					{
						var deployParameters = new DeployPrivateDnsZoneGroupsArmTemplateParameters
						{
							SubscriptionId = orchestratorParameters.SubscriptionId,
							PrivateEndpointResourceId = orchestratorParameters.ResourceId,
							PrivateDnsZone = privateDnsZone
						};

						if (!await context.CallActivityAsync<bool>(nameof(DeployPrivateDnsZoneGroupsArmTemplate), deployParameters))
						{
							return false;
						}

						addedDnsZones.Add(privateDnsZone);
					}
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

		[FunctionName(nameof(DeployPrivateDnsZoneGroupsArmTemplate))]
		public async Task<bool> DeployPrivateDnsZoneGroupsArmTemplate([ActivityTrigger] DeployPrivateDnsZoneGroupsArmTemplateParameters parameters, ILogger log)
		{
			var privateEndpointResourceGroupName = Constants.ResourceGroupCaptureRegEx.Match(parameters.PrivateEndpointResourceId).Groups["resourcegroup"].Value;
			var privateEndpointName = Constants.PrivateEndpointCaptureRegEx.Match(parameters.PrivateEndpointResourceId).Groups["privateEndpoint"].Value;
			var privateDnsZoneResourceId = privateDnsManagementService.GetPrivateDnsZoneResourceId(parameters.PrivateDnsZone);

			resourceManagementService.SetSubscriptionId(parameters.SubscriptionId);
			return await resourceManagementService.CreatePrivateDnsZoneGroupsDeploymentAsync(privateEndpointResourceGroupName, privateEndpointName, privateDnsZoneResourceId);
		}

	}
}
