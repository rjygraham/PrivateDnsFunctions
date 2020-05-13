using Microsoft.Azure.Management.PrivateDns.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Rgom.PrivateDns.Functions.Data;
using Rgom.PrivateDns.Functions.Models;
using Rgom.PrivateDns.Functions.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using DnsRecordSet = Microsoft.Azure.Management.PrivateDns.Models.RecordSet;


namespace Rgom.PrivateDns.Functions
{
	public class SharedDurableFunctions
	{
		private readonly IPrivateDnsManagementService privateDnsManagementService;
		private readonly IDnsEntityService dnsEntityService;

		public SharedDurableFunctions(IPrivateDnsManagementService privateDnsManagementService, IDnsEntityService dnsEntityService)
		{
			this.privateDnsManagementService = privateDnsManagementService;
			this.dnsEntityService = dnsEntityService;
		}

		[FunctionName(nameof(CreateDnsRecordSetAsync))]
		public async Task<bool> CreateDnsRecordSetAsync([ActivityTrigger] DnsParameters parameters, ILogger log)
		{
			var newRecordSet = new DnsRecordSet(aRecords: new List<ARecord> { new ARecord(parameters.IpAddress) }, ttl: 3600);
			var savedRecordSet = await privateDnsManagementService.CreateOrUpdateAsync(parameters.DnsZone, parameters.RecordType, parameters.Hostname, newRecordSet);

			return !(savedRecordSet is null);
		}

		[FunctionName(nameof(CreateDnsEntityAsync))]
		public async Task<bool> CreateDnsEntityAsync([ActivityTrigger] DnsParameters parameters, ILogger log)
		{
			var dnsEntity = new DnsEntity(parameters.ResourceId, parameters.Hostname, parameters.DnsZone, RecordType.A, parameters.IpAddress);
			return await dnsEntityService.InsertOrUpdateDnsEntityAsync(dnsEntity);
		}

		[FunctionName(nameof(ListDnsEntitiesAsync))]
		public async Task<List<DnsEntity>> ListDnsEntitiesAsync([ActivityTrigger] string resourceId, ILogger log)
		{
			return await dnsEntityService.ListDnsEntitiesAsync(resourceId);
		}

		[FunctionName(nameof(DeleteDnsRecordSetAsync))]
		public async Task<bool> DeleteDnsRecordSetAsync([ActivityTrigger] DnsParameters parameters, ILogger log)
		{
			return await privateDnsManagementService.DeleteAsync(parameters.DnsZone, parameters.RecordType, parameters.Hostname);
		}

		[FunctionName(nameof(DeleteDnsEntityAsync))]
		public async Task<bool> DeleteDnsEntityAsync([ActivityTrigger] DnsEntity dnsEntity, ILogger log)
		{
			return await dnsEntityService.DeleteDnsEntityAsync(dnsEntity);
		}
	}
}
