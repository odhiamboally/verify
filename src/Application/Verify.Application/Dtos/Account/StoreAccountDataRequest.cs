using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Verify.Application.Dtos.Account;
public record class StoreAccountDataRequest
{
    public required byte[] AccountHash { get; init; }
    public required string BankBIC { get; init; }
    public required string AccountNumber { get; init; }
    public required string AccountName { get; init; }
}
