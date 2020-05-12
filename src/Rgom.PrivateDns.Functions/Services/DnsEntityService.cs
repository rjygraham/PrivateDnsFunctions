using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Rgom.PrivateDns.Functions.Data;
using System;
using System.Collections.Generic;
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

				return client.GetTableReference(Constants.DnsEntitiesTableName);
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
				throw;
			}
		}

		public async Task<List<DnsEntity>> ListDnsEntitiesAsync(string resourceId)
		{
			var query = new TableQuery<DnsEntity>().Where(
				TableQuery.GenerateFilterCondition(
					"PartitionKey",
					QueryComparisons.Equal,
					resourceId.Replace("/", ":").ToLower()
				)
			);

			var entities = new List<DnsEntity>();

			TableQuerySegment<DnsEntity> segment = null;

			while (segment == null || segment.ContinuationToken != null)
			{
				segment = await table.Value.ExecuteQuerySegmentedAsync(query, segment?.ContinuationToken);
				entities.AddRange(segment);
			}

			return entities;
		}

		public async Task<bool> DeleteDnsEntityAsync(DnsEntity entity)
		{
			try
			{
				TableOperation deleteOperation = TableOperation.Delete(entity);
				TableResult result = await table.Value.ExecuteAsync(deleteOperation);
				return result.HttpStatusCode == (int)HttpStatusCode.Accepted;
			}
			catch (StorageException ex)
			{
				throw;
			}
		}
	}
}
