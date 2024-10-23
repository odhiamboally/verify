// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Web.Client.Console.ApiClients;
using Web.Client.Console.Configurations;
using Web.Client.Console.Dtos;


var serviceCollection = new ServiceCollection();

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)  // Set the base path to the current directory
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// Bind configuration settings to a POCO class (optional)
var appSettings = configuration.GetSection("AppSettings").Get<AppSettings>();

serviceCollection.AddHttpClient("DHT", client =>
    {
        client.BaseAddress = new Uri(appSettings!.ApiBaseUrl!);
        client.Timeout = TimeSpan.FromSeconds(appSettings.TimeoutSeconds);
    });

serviceCollection.AddSingleton<IConfiguration>(configuration);
serviceCollection.AddSingleton<IApiClient, ApiClient>();

var serviceProvider =  serviceCollection.BuildServiceProvider();

var config = serviceProvider.GetRequiredService<IConfiguration>();
var apiClient = serviceProvider.GetRequiredService<IApiClient>();



AccountRequest accountRequest = new()
{
    InitiatorBIC = "SCBLKENX",
    RecipientBIC = "BARCKENX",
    RecipientAccountNumber = "2456345645"
};

var verifyResponse = await Methods.FetchAccountData(apiClient, accountRequest);
Console.WriteLine($"Account Holder: {verifyResponse.AccountName}");
Console.WriteLine();
Console.WriteLine("Account Number: " + verifyResponse.AccountNumber);
Console.WriteLine();
Console.ReadKey();



public class Methods
{
    public async static Task<AccountInfo> FetchAccountData(IApiClient apiClient, AccountRequest request)
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