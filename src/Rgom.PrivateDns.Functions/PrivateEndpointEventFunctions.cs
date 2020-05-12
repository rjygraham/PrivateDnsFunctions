using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rgom.PrivateDns.Functions.Handlers;
using System;
using System.Threading.Tasks;

namespace Rgom.PrivateDns.Functions
{
	public class PrivateEndpointEventFunctions
    {
		private readonly IPrivateEndpointEventHandler eventHandler;

		public PrivateEndpointEventFunctions(IPrivateEndpointEventHandler eventHandler)
		{
			this.eventHandler = eventHandler ?? throw new ArgumentNullException(nameof(eventHandler));
		}

		[FunctionName(nameof(HandlePrivateEndpointEventsAsync))]
        public async Task HandlePrivateEndpointEventsAsync([EventGridTrigger]EventGridEvent eventGridEvent, ILogger log)
        {
			string eventType = eventGridEvent.EventType;
			dynamic data = eventGridEvent.Data;
			string subscriptionId = data.subscriptionId;
			string resourceId = eventGridEvent.Subject;

			switch (eventType)
			{
				case "Microsoft.Resources.ResourceWriteSuccess":
					await eventHandler.HandlePrivateEndpointCreatedEventAsync(subscriptionId, resourceId);
					break;
				case "Microsoft.Resources.ResourceDeleteSuccess":
					await eventHandler.HandlePrivateEndpointDeletedEventAsync(subscriptionId, resourceId);
					break;
				default:
					throw new Exception();
			}
		}
    }
}
