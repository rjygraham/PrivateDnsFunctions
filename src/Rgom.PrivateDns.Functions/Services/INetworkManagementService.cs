using Microsoft.Azure.Management.Network.Models;
using System.Threading.Tasks;

namespace Rgom.PrivateDns.Functions.Services
{
	public interface INetworkManagementService
	{
		void SetSubscriptionId(string subscriptionId);

		Task<NetworkInterface> GetNetworkInterfaceAsync(string resourceGroupName, string networkInterfaceName);

		Task<PrivateEndpoint> GetPrivateEndpointAsync(string resourceGroupName, string privateEndpointName);
	}
}
