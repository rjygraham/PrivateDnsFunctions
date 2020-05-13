#if DEBUG

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Rgom.PrivateDns.Functions.Models;
using System;
using System.Threading.Tasks;
using System.Web.Http;

namespace Rgom.PrivateDns.Functions
{
	public class LocalTestFunctions
	{
		[FunctionName(nameof(TestHandleNetworkInterfaceEventsAsync))]
		public async Task<IActionResult> TestHandleNetworkInterfaceEventsAsync(
			[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] EventGridEvent eventGridEvent,
			[DurableClient] IDurableOrchestrationClient starter,
			ILogger log
		)
		{
			try
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
						instanceId = await starter.StartNewAsync(nameof(NetworkInterfaceEventFunctions.OrchestrateNetworkInterfaceCreatedAsync), eventGridEvent.Id, durableParameters);
						break;
					case "Microsoft.Resources.ResourceDeleteSuccess":
						instanceId = await starter.StartNewAsync(nameof(NetworkInterfaceEventFunctions.OrchestrateNetworkInterfaceDeletedAsync), eventGridEvent.Id, durableParameters);
						break;
					default:
						throw new Exception();
				}
				return new OkResult();
			}
			catch (Exception ex)
			{
				return new InternalServerErrorResult();
			}
		}

		[FunctionName(nameof(TestHandlePrivateEndpointEventsAsync))]
		public async Task<IActionResult> TestHandlePrivateEndpointEventsAsync(
			[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] EventGridEvent eventGridEvent,
			[DurableClient] IDurableOrchestrationClient starter, 
			ILogger log
		)
		{
			try
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
						instanceId = await starter.StartNewAsync(nameof(PrivateEndpointEventFunctions.OrchestratePrivateEndpointCreatedAsync), eventGridEvent.Id, durableParameters);
						break;
					case "Microsoft.Resources.ResourceDeleteSuccess":
						instanceId = await starter.StartNewAsync(nameof(PrivateEndpointEventFunctions.OrchestratePrivateEndpointDeletedAsync), eventGridEvent.Id, durableParameters);
						break;
					default:
						throw new Exception();
				}
				return new OkResult();
			}
			catch (Exception ex)
			{
				return new InternalServerErrorResult();
			}
		}
	}
}

#endif
