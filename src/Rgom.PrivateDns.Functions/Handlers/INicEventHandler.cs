using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Rgom.PrivateDns.Functions.Handlers
{
	public interface INicEventHandler
	{
		Task<bool> HandleNicCreatedEventAsync(string subscriptionId, string resourceId);
		Task<bool> HandleNicDeletedEventAsync(string subscriptionId, string resourceId);
	}
}
