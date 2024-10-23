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
    Task<Response<AccountInfo>> CreateAsync(StoreAccountDataRequest request);
    Task<Response<AccountInfo>> DeleteAsync(int Id);
    Task<Response<List<AccountInfo>>> FindAllAsync(PaginationSetting paginationSetting);
    Task<Response<AccountInfo>> FindByIdAsync(int Id);
    Task<Response<List<AccountInfo>>> SearchAsync(SearchRequest searchRequest);
    Task<Response<AccountInfo>> UpdateAsync(UpdateAccountRequest request, bool dBWins);
}
