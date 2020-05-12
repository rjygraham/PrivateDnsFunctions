using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Rest;
using System;
using System.Threading.Tasks;

namespace Rgom.PrivateDns.Functions.Services
{
	internal class CredentialService : ICredentialService
	{
		private readonly string tenantId;
		private readonly AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();

		public CredentialService(string tenantId)
		{
			this.tenantId = tenantId;
		}

		public async Task<TokenCredentials> GetTokenCredentialsAsync()
		{
			try
			{
				var authResult = await azureServiceTokenProvider.GetAuthenticationResultAsync("https://management.azure.com/", tenantId).ConfigureAwait(false);
				return new TokenCredentials(authResult.AccessToken);
			}
			catch (Exception)
			{
				throw;
			}
		}

	}
}
