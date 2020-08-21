using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Rgom.PrivateDns.Functions.Data;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Rgom.PrivateDns.Functions.Services
{
	internal class DnsEntityService : IDnsEntityService
	{
		private readonly Lazy<CloudTable> table;

		public DnsEntityService(string connectionString)
		{
			table = new Lazy<CloudTable>(() =>
			{
				var storageAccount = CloudStorageAccount.Parse(connectionString);
				var client = storageAccount.CreateCloudTableClient();

				var table = client.GetTableReference(Constants.DnsEntitiesTableName);
				table.CreateIfNotExistsAsync().GetAwaiter().GetResult();

				return table;
			});
		}

		public async Task<bool> InsertOrUpdateDnsEntityAsync(DnsEntity entity)
		{
			try
			{
				TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(entity);
				TableResult result = await table.Value.ExecuteAsync(insertOrMergeOperation);
				return result.HttpStatusCode == (int)HttpStatusCode.NoContent;
			}
			catch (StorageException ex)
			{
				return false;
			}
		}

		public async Task<DnsEntity> GetDnsEntityAsync(string resourceId)
		{
			var entity = new DnsEntity(resourceId);

			var retrieveOperation = TableOperation.Retrieve<DnsEntity>(entity.PartitionKey, entity.RowKey);

			var result = await table.Value.ExecuteAsync(retrieveOperation);

			if (result.HttpStatusCode == (int)HttpStatusCode.OK)
			{
				return (DnsEntity)result.Result;
			}

			return null;
		}

		public async Task<bool> DeleteDnsEntityAsync(DnsEntity entity)
		{
			try
			{
				TableOperation deleteOperation = TableOperation.Delete(entity);
				TableResult result = await table.Value.ExecuteAsync(deleteOperation);
				return result.HttpStatusCode == (int)HttpStatusCode.NoContent;
			}
			catch (StorageException ex)
			{
				return false;
			}
		}
	}
}
