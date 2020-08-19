using Microsoft.Azure.Management.PrivateDns.Models;
using System.Threading.Tasks;

namespace Rgom.PrivateDns.Functions.Services
{
	public interface IPrivateDnsManagementService
	{
		string GetPrivateDnsZoneResourceId(string privateDnsZone);
		Task<RecordSet> CreateOrUpdateAsync(string privateZoneName, RecordType recordType, string relativeRecordSetName, RecordSet parameters);
		Task<bool> DeleteAsync(string privateZoneName, RecordType recordType, string relativeRecordSetName);
	}
}
