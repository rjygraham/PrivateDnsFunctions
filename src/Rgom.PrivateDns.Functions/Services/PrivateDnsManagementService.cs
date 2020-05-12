using Microsoft.Azure.Management.PrivateDns;
using Microsoft.Azure.Management.PrivateDns.Models;
using Microsoft.Rest;
using System;
using System.Threading.Tasks;

namespace Rgom.PrivateDns.Functions.Services
{
	internal class PrivateDnsManagementService : IPrivateDnsManagementService
	{
		private readonly Lazy<PrivateDnsManagementClient> client;
		private readonly string privateDnsResourceGroupName;

		public PrivateDnsManagementService(TokenCredentials credentials, string privateDnsSubscriptionId, string privateDnsResourceGroupName)
		{
			client = new Lazy<PrivateDnsManagementClient>(() =>
			{
				var result = new PrivateDnsManagementClient(credentials);
				result.SubscriptionId = privateDnsSubscriptionId;

				return result;
			});

			this.privateDnsResourceGroupName = privateDnsResourceGroupName;
		}

		public async Task<RecordSet> CreateOrUpdateAsync(string privateZoneName, RecordType recordType, string relativeRecordSetName, RecordSet parameters)
		{
			return await client.Value.RecordSets.CreateOrUpdateAsync(privateDnsResourceGroupName, privateZoneName, recordType, relativeRecordSetName, parameters);
		}

		public async Task DeleteAsync(string privateZoneName, RecordType recordType, string relativeRecordSetName)
		{
			await client.Value.RecordSets.DeleteAsync(privateDnsResourceGroupName, privateZoneName, recordType, relativeRecordSetName);
		}
	}
}
