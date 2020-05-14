using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Rest;
using Rgom.PrivateDns.Functions.Services;
using System;

[assembly: FunctionsStartup(typeof(Rgom.PrivateDns.Functions.Startup))]

namespace Rgom.PrivateDns.Functions
{
	public class Startup : FunctionsStartup
	{
		public override void Configure(IFunctionsHostBuilder builder)
		{
			builder.Services.AddSingleton<ICredentialService, CredentialService>(sp => new CredentialService(Environment.GetEnvironmentVariable("TenantId")));

			builder.Services.AddSingleton<IDnsEntityService, DnsEntityService>(sp => new DnsEntityService(Environment.GetEnvironmentVariable("AzureWebJobsStorage")));

			builder.Services.AddScoped(sp => sp.GetService<ICredentialService>().GetTokenCredentialsAsync().GetAwaiter().GetResult());
			builder.Services.AddScoped<INetworkManagementService, NetworkManagementService>(sp =>
			{
				return new NetworkManagementService(sp.GetService<TokenCredentials>());
			});

			builder.Services.AddScoped<IPrivateDnsManagementService, PrivateDnsManagementService>(sp => new PrivateDnsManagementService
				(
					sp.GetService<TokenCredentials>(),
					Environment.GetEnvironmentVariable("PrivateDnsSubscriptionId"),
					Environment.GetEnvironmentVariable("PrivateDnsResourceGroupName")
				)
			);
		}
    }
}
