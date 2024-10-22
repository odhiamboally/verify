using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verify.Application.Dtos.Account;
using Verify.Application.Dtos.Bank;
using Verify.Application.Dtos.Common;

namespace Verify.Application.Abstractions.DHT;
public interface IReplicationService
{
    Task<DHTResponse<bool>> ReplicateAccountDataAsync(ReplicateAccountDataRequest replicateAccountDataRequest);
    Task<DHTResponse<bool>> HandleNodeFailureAsync(NodeFailureRequest nodeFailureRequest);

}
