using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Verify.Application.Dtos.Bank;
public record BroadcastNodePresenceRequest
{
    public required byte[] BankHash { get; init; }
    public required NodeInfo AddNodeRoutingTableRequest { get; init; }
}
