using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verify.Application.Abstractions.IServices;
using Verify.Application.Dtos.Account;
using Verify.Application.Dtos.Common;


namespace Verify.Infrastructure.Implementations.Services;
internal sealed class AccountService : IAccountService
{
    public AccountService()
    {
        
    }

    public Task<Response<AccountResponse>> CreateAsync(StoreAccountDataRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<Response<AccountResponse>> DeleteAsync(int Id)
    {
        throw new NotImplementedException();
    }

    public Task<Response<List<AccountResponse>>> FindAllAsync(PaginationSetting paginationSetting)
    {
        throw new NotImplementedException();
    }

    public Task<Response<AccountResponse>> FindByIdAsync(int Id)
    {
        throw new NotImplementedException();
    }

    public Task<Response<List<AccountResponse>>> SearchAsync(SearchRequest searchRequest)
    {
        throw new NotImplementedException();
    }

    public Task<Response<AccountResponse>> UpdateAsync(UpdateAccountRequest request, bool dBWins)
    {
        throw new NotImplementedException();
    }
}
