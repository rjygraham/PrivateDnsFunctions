using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Rgom.PrivateDns.Functions.Handlers;
using System;
using System.Threading.Tasks;

namespace Rgom.PrivateDns.Functions
{
	public class NicEventFunctions
    {
		private readonly INicEventHandler eventHandler;

		public NicEventFunctions(INicEventHandler eventHandler)
		{
			this.eventHandler = eventHandler ?? throw new ArgumentNullException(nameof(eventHandler));
		}

		[FunctionName(nameof(HandleNicEventsAsync))]
		public async Task HandleNicEventsAsync([EventGridTrigger]EventGridEvent eventGridEvent, ILogger log)
		{
			string eventType = eventGridEvent.EventType;
			dynamic data = eventGridEvent.Data;
			string subscriptionId = data.subscriptionId;
			string resourceId = eventGridEvent.Subject;

			switch (eventType)
			{
				case "Microsoft.Resources.ResourceWriteSuccess":
					await eventHandler.HandleNicCreatedEventAsync(subscriptionId, resourceId);
					break;
				case "Microsoft.Resources.ResourceDeleteSuccess":
					await eventHandler.HandleNicDeletedEventAsync(subscriptionId, resourceId);
					break;
				default:
					throw new Exception();
			}
		}
	}
}
