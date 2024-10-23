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

    public Task<Response<AccountInfo>> CreateAsync(StoreAccountDataRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<Response<AccountInfo>> DeleteAsync(int Id)
    {
        throw new NotImplementedException();
    }

    public Task<Response<List<AccountInfo>>> FindAllAsync(PaginationSetting paginationSetting)
    {
        throw new NotImplementedException();
    }

    public Task<Response<AccountInfo>> FindByIdAsync(int Id)
    {
        throw new NotImplementedException();
    }

    public Task<Response<List<AccountInfo>>> SearchAsync(SearchRequest searchRequest)
    {
        throw new NotImplementedException();
    }

    public Task<Response<AccountInfo>> UpdateAsync(UpdateAccountRequest request, bool dBWins)
    {
        throw new NotImplementedException();
    }
}
