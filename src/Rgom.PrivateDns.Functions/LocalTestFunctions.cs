#if DEBUG

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Rgom.PrivateDns.Functions.Handlers;
using System;
using System.Threading.Tasks;
using System.Web.Http;

namespace Rgom.PrivateDns.Functions
{
	public class LocalTestFunctions
	{
		private readonly NicEventFunctions nicFunctions;
		private readonly PrivateEndpointEventFunctions privateEndpointFunctions;
		
		public LocalTestFunctions(INicEventHandler nicEventHandler, IPrivateEndpointEventHandler privateEndpointEventHandler)
		{
			nicFunctions = new NicEventFunctions(nicEventHandler);
			privateEndpointFunctions = new PrivateEndpointEventFunctions(privateEndpointEventHandler);
		}

		[FunctionName(nameof(TestHandleNicEventsAsync))]
		public async Task<IActionResult> TestHandleNicEventsAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] EventGridEvent eventGridEvent, ILogger log)
		{
			try
			{
				await nicFunctions.HandleNicEventsAsync(eventGridEvent, log);
				return new OkResult();
			}
			catch (Exception ex)
			{
				return new InternalServerErrorResult();
			}
		}

		[FunctionName(nameof(TestHandlePrivateEndpointEventsAsync))]
		public async Task<IActionResult> TestHandlePrivateEndpointEventsAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] EventGridEvent eventGridEvent, ILogger log)
		{
			try
			{
				await privateEndpointFunctions.HandlePrivateEndpointEventsAsync(eventGridEvent, log);
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
