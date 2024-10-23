using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

using Web.Client.Console.Dtos;

namespace Web.Client.Console.ApiClients;
internal sealed class ApiClient : IApiClient
{
    private readonly HttpClient httpClient;
    public ApiClient(IHttpClientFactory httpClientFactory)
    {
        httpClient = httpClientFactory.CreateClient();
    }

    public async Task<AccountResponse> FetchAccountData(AccountRequest request)
    {
        return await httpClient.PostAsJsonAsync();
    }

}
