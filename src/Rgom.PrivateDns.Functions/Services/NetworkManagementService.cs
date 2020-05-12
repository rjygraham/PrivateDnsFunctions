using Microsoft.Azure.Management.Network;
using Microsoft.Azure.Management.Network.Models;
using Microsoft.Rest;
using System;
using System.Threading.Tasks;

namespace Rgom.PrivateDns.Functions.Services
{
	internal class NetworkManagementService : INetworkManagementService
	{
		private readonly Lazy<NetworkManagementClient> client;
		public NetworkManagementService(TokenCredentials credentials)
		{
			client = new Lazy<NetworkManagementClient>(() =>
			{
				var result = new NetworkManagementClient(credentials);
				return result;
			});
		}

		public void SetSubscriptionId(string subscriptionId)
		{
			client.Value.SubscriptionId = subscriptionId;
		}

		public async Task<NetworkInterface> GetNetworkInterfaceAsync(string resourceGroupName, string networkInterfaceName)
		{
			return await client.Value.NetworkInterfaces.GetAsync(resourceGroupName, networkInterfaceName);
		}

		public async Task<PrivateEndpoint> GetPrivateEndpointAsync(string resourceGroupName, string privateEndpointName)
		{
			return await client.Value.PrivateEndpoints.GetAsync(resourceGroupName, privateEndpointName);
		}

	}
}
