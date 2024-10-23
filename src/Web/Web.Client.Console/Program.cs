// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using Web.Client.Console.ApiClients;
using Web.Client.Console.Dtos;


// Create service provider with IHttpClientFactory
var serviceProvider = new ServiceCollection()
    .AddHttpClient()
    .AddSingleton<IApiClient, ApiClient>()
    .BuildServiceProvider();

// Resolve ApiClient from the service provider
var apiClient = serviceProvider.GetRequiredService<IApiClient>();

AccountRequest accountRequest = new()
{
    InitiatorBIC = "SCBLKENX",
    RecipientBIC = "BARCKENX",
    RecipientAccountNumber = "2456345645"
};

var verifyResponse = await Methods.FetchAccountData(apiClient, accountRequest);
Console.WriteLine("Account Holder: " + verifyResponse.FirstName + " " + verifyResponse.LastName);
Console.WriteLine("Account Number: " + verifyResponse.AccountNumber);



public class Methods
{
    public async static Task<AccountResponse> FetchAccountData(IApiClient apiClient, AccountRequest request)
    {
		try
		{
            // Make the API call and return the response
            var accountResponse = await apiClient.FetchAccountData(request);
            return accountResponse;
        }
		catch (Exception)
		{

			throw;
		}
    }
}