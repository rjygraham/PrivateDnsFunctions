using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Rgom.PrivateDns.Functions.Handlers
{
	public interface IPrivateEndpointEventHandler
	{
		Task<bool> HandlePrivateEndpointCreatedEventAsync(string subscriptionId, string resourceId);
		Task<bool> HandlePrivateEndpointDeletedEventAsync(string subscriptionId, string resourceId);
	}
}
