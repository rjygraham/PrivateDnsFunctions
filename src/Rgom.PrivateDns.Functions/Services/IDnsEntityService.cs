using Rgom.PrivateDns.Functions.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Rgom.PrivateDns.Functions.Services
{
	public interface IDnsEntityService
	{
		Task<bool> InsertOrUpdateDnsEntityAsync(DnsEntity entity);
		Task<DnsEntity> GetDnsEntityAsync(string resourceId);
		Task<bool> DeleteDnsEntityAsync(DnsEntity entity);
	}
}
