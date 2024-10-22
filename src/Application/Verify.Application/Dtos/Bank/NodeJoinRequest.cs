using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Verify.Application.Dtos.Bank;
public record NodeJoinRequest
{
    public required string BankBIC { get; init; }
    public required Uri BootstrapNodeUri { get; init; }
}
