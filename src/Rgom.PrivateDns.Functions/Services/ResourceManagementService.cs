using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.Rest;
using System;
using System.Threading.Tasks;

namespace Rgom.PrivateDns.Functions.Services
{
	public class ResourceManagementService: IResourceManagementService
	{
		private readonly Lazy<ResourceManagementClient> client;

		public ResourceManagementService(TokenCredentials credentials)
		{
			client = new Lazy<ResourceManagementClient>(() =>
			{
				var result = new ResourceManagementClient(credentials);
				return result;
			});
		}

		public void SetSubscriptionId(string subscriptionId)
		{
			client.Value.SubscriptionId = subscriptionId;
		}

		public async Task<bool> CreatePrivateDnsZoneGroupsDeploymentAsync(string resourceGroupName, string privateEndpointName, string privateDnsZoneResourceId)
		{
			var formattedTemplate = string.Format(Constants.PrivateDnsZoneGroupsArmTemplateFormat, privateEndpointName, privateDnsZoneResourceId);
			var deployment = new Deployment(new DeploymentProperties(DeploymentMode.Incremental, template: formattedTemplate));

			var result = await client.Value.Deployments.CreateOrUpdateAsync(resourceGroupName, $"pedns.{DateTime.Now.ToString("yyyyMMdd-HHmmss")}", deployment);
			
			var succeededState = result?.Properties?.ProvisioningState.Equals("Succeeded", StringComparison.OrdinalIgnoreCase);
			return succeededState.HasValue && succeededState.Value;
		}
	}
}
