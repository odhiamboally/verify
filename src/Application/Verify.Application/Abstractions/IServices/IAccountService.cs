using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verify.Application.Dtos.Account;
using Verify.Application.Dtos.Common;

namespace Verify.Application.Abstractions.IServices;
public interface IAccountService
{
    Task<Response<AccountResponse>> CreateAsync(StoreAccountDataRequest request);
    Task<Response<AccountResponse>> DeleteAsync(int Id);
    Task<Response<List<AccountResponse>>> FindAllAsync(PaginationSetting paginationSetting);
    Task<Response<AccountResponse>> FindByIdAsync(int Id);
    Task<Response<List<AccountResponse>>> SearchAsync(SearchRequest searchRequest);
    Task<Response<AccountResponse>> UpdateAsync(UpdateAccountRequest request, bool dBWins);
}
