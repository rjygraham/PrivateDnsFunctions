using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Rest;
using Rgom.PrivateDns.Functions.Handlers;
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
				var result = new NetworkManagementService(sp.GetService<TokenCredentials>());
				return result;
			});

			builder.Services.AddScoped<IPrivateDnsManagementService, PrivateDnsManagementService>(sp =>
			{
				var result = new PrivateDnsManagementService(sp.GetService<TokenCredentials>(), Environment.GetEnvironmentVariable("PrivateDnsSubscription"), Environment.GetEnvironmentVariable("PrivateDnsResourceGroup"));
				return result;
			});

			builder.Services.AddScoped<IPrivateEndpointEventHandler, PrivateEndpointEventHandler>();

			builder.Services.AddScoped<INicEventHandler, NicEventHandler>(sp =>
			{
				var result = new NicEventHandler(sp.GetService<INetworkManagementService>(), sp.GetService<IPrivateDnsManagementService>(), sp.GetService<IDnsEntityService>(), Environment.GetEnvironmentVariable("DefaultPrivateDnsZone"), Environment.GetEnvironmentVariable("HostNameTagName"));
				return result;
			});
		}
    }
}
