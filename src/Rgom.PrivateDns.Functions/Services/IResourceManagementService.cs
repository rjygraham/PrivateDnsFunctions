using System.Threading.Tasks;

namespace Rgom.PrivateDns.Functions.Services
{
	public interface IResourceManagementService
	{
		void SetSubscriptionId(string subscriptionId);

		Task<bool> CreatePrivateDnsZoneGroupsDeploymentAsync(string resourceGroupName, string privateEndpointName, string privateDnsZoneResourceId);
	}
}
