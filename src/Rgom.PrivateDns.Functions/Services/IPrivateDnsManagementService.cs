﻿using Microsoft.Azure.Management.PrivateDns.Models;
using System.Threading.Tasks;

namespace Rgom.PrivateDns.Functions.Services
{
	public interface IPrivateDnsManagementService
	{
		Task<RecordSet> CreateOrUpdateAsync(string privateZoneName, RecordType recordType, string relativeRecordSetName, RecordSet parameters);
		Task DeleteAsync(string privateZoneName, RecordType recordType, string relativeRecordSetName);
	}
}
