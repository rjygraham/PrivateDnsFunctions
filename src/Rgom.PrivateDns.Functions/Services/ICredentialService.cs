using Microsoft.Rest;
using System.Threading.Tasks;

namespace Rgom.PrivateDns.Functions.Services
{
	public interface ICredentialService
	{
		Task<TokenCredentials> GetTokenCredentialsAsync();
	}
}
