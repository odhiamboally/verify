using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Refit;

namespace Verify.Infrastructure.Utilities.DHT.ApiClients;
internal sealed class ApiClientFactory : IApiClientFactory
{
    private readonly RefitSettings refitSettings;

    public ApiClientFactory(RefitSettings RefitSettings)
    {
        refitSettings = RefitSettings;

    }

    public IApiClient CreateClient(string nodeBaseUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(nodeBaseUrl))
            {
                throw new ArgumentException("Bank base URL cannot be null or empty", nameof(nodeBaseUrl));
            }

            // Create and return a Refit client dynamically
            return RestService.For<IApiClient>(new HttpClient
            {
                BaseAddress = new Uri(nodeBaseUrl)
            }, refitSettings);
        }
        catch (Exception)
        {

            throw;
        }
    }
}
