using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Refit;
using Verify.Application.Dtos.Account;
using Verify.Application.Dtos.Common;

namespace Verify.Infrastructure.Utilities.DHT.ApiClients;
internal interface IApiClient
{
    [Post("/fetchaccountdata")]
    Task<DHTResponse<AccountInfo>> FetchAccountData([Body] AccountRequest fetchAccountRequest);




}
