using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Verify.Application.Dtos.Account;
public record ReplicateAccountDataRequest
{
    public required byte[] AccountHash { get; init; }
    public required Uri NodeUri { get; init; }
    public required StoreAccountDataRequest AccountData { get; init; }
}
