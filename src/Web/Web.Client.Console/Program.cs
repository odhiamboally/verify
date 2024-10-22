// See https://aka.ms/new-console-template for more information
using Web.Client.Console.Dtos;



AccountRequest accountRequest = new()
{
    InitiatorBIC = "SCB",
    RecipientBIC = "ABSA",
    RecipientAccountNumber = "2456345645"
};

var verifyResponse = Methods.FetchAccountData(accountRequest);

Console.WriteLine("Hello, World!");






public class Methods
{
    public async static Task<AccountResponse> FetchAccountData(AccountRequest request)
    {
		try
		{

		}
		catch (Exception)
		{

			throw;
		}
    }
}