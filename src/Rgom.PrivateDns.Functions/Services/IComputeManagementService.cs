using Microsoft.Azure.Management.Compute.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Rgom.PrivateDns.Functions.Services
{
	public interface IComputeManagementService
	{
		void SetSubscriptionId(string subscriptionId);

		Task<VirtualMachine> GetVirtualMachineAsync(string resourceGroupName, string vmName);
	}
}
