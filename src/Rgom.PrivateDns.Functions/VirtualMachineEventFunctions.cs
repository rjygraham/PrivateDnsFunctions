using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Rgom.PrivateDns.Functions.Models;
using Rgom.PrivateDns.Functions.Services;
using System;
using System.Threading.Tasks;

namespace Rgom.PrivateDns.Functions
{
	public class VirtualMachineEventFunctions
	{
		private readonly IComputeManagementService computeManagementService;

		public VirtualMachineEventFunctions(IComputeManagementService computeManagementService)
		{
			this.computeManagementService = computeManagementService;
		}

		[FunctionName(nameof(HandleVirtualMachineEventsAsync))]
		public async Task HandleVirtualMachineEventsAsync(
			[EventGridTrigger] EventGridEvent eventGridEvent,
			[DurableClient] IDurableOrchestrationClient starter,
			ILogger log
		)
		{
			dynamic data = eventGridEvent.Data;
			string subscriptionId = data.subscriptionId;
			string operationName = data.operationName;
			string resourceId = eventGridEvent.Subject;

			var durableParameters = new OrchestratorParameters
			{
				SubscriptionId = subscriptionId,
				ResourceId = resourceId
			};

			string instanceId;

			switch (operationName)
			{
				case "Microsoft.Compute/virtualMachines/start/action":
					instanceId = await starter.StartNewAsync(nameof(OrchestrateVirtualMachineStartedAsync), eventGridEvent.Id, durableParameters);
					break;
				case "Microsoft.Compute/virtualMachines/deallocate/action":
					instanceId = await starter.StartNewAsync(nameof(OrchestrateVirtualMachineDeallocatedAsync), eventGridEvent.Id, durableParameters);
					break;
				default:
					throw new Exception();
			}

			log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
		}

		[FunctionName(nameof(OrchestrateVirtualMachineStartedAsync))]
		public async Task<bool> OrchestrateVirtualMachineStartedAsync([OrchestrationTrigger] IDurableOrchestrationContext context)
		{
			var orchestratorParameters = context.GetInput<OrchestratorParameters>();

			// Get VM that was just started.
			var vm = await context.CallActivityAsync<VirtualMachine>(nameof(GetVirtualMachineAsync), orchestratorParameters);

			foreach (var nic in vm.NetworkProfile.NetworkInterfaces)
			{
				var nicOrchestratorParameters = new OrchestratorParameters
				{
					SubscriptionId = orchestratorParameters.SubscriptionId,
					ResourceId = nic.Id
				};

				await context.CallSubOrchestratorAsync(nameof(NetworkInterfaceEventFunctions.OrchestrateNetworkInterfaceCreatedAsync), nicOrchestratorParameters);
			}

			return true;
		}

		[FunctionName(nameof(OrchestrateVirtualMachineDeallocatedAsync))]
		public async Task<bool> OrchestrateVirtualMachineDeallocatedAsync([OrchestrationTrigger] IDurableOrchestrationContext context)
		{
			var orchestratorParameters = context.GetInput<OrchestratorParameters>();

			// Get VM that was just deallocated.
			var vm = await context.CallActivityAsync<VirtualMachine>(nameof(GetVirtualMachineAsync), orchestratorParameters);

			foreach (var nic in vm.NetworkProfile.NetworkInterfaces)
			{
				var nicOrchestratorParameters = new OrchestratorParameters
				{
					SubscriptionId = orchestratorParameters.SubscriptionId,
					ResourceId = nic.Id
				};

				await context.CallSubOrchestratorAsync(nameof(NetworkInterfaceEventFunctions.OrchestrateNetworkInterfaceDeletedAsync), nicOrchestratorParameters);
			}

			return true;
		}

		[FunctionName(nameof(GetVirtualMachineAsync))]
		public async Task<VirtualMachine> GetVirtualMachineAsync([ActivityTrigger] OrchestratorParameters parameters, ILogger log)
		{
			var resourceGroupName = Constants.ResourceGroupCaptureRegEx.Match(parameters.ResourceId).Groups["resourcegroup"].Value;
			var vmName = Constants.VmCaptureRegEx.Match(parameters.ResourceId).Groups["vm"].Value;

			computeManagementService.SetSubscriptionId(parameters.SubscriptionId);
			return await computeManagementService.GetVirtualMachineAsync(resourceGroupName, vmName);
		}
	}
}
