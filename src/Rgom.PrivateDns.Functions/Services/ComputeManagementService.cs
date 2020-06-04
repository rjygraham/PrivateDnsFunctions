using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Rest;
using System;
using System.Threading.Tasks;

namespace Rgom.PrivateDns.Functions.Services
{
	class ComputeManagementService : IComputeManagementService
	{
		private readonly Lazy<ComputeManagementClient> client;

		public ComputeManagementService(TokenCredentials credentials)
		{
			client = new Lazy<ComputeManagementClient>(() =>
			{
				var result = new ComputeManagementClient(credentials);
				return result;
			});
		}

		public void SetSubscriptionId(string subscriptionId)
		{
			client.Value.SubscriptionId = subscriptionId;
		}

		public async Task<VirtualMachine> GetVirtualMachineAsync(string resourceGroupName, string vmName)
		{
			return await client.Value.VirtualMachines.GetAsync(resourceGroupName, vmName);
		}
	}
}
