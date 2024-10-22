using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Verify.Application.Dtos.Account;
using Verify.Application.Dtos.Bank;
using Verify.Application.Dtos.Common;

namespace Verify.Application.Abstractions.DHT;
public interface IDHTService
{
    
    Task<DHTResponse<AccountResponse>> StoreAccountDataAsync(StoreAccountDataRequest storeAccountDataRequest);
    Task<DHTResponse<AccountResponse>> LookupAccountInMemoryAsync(AccountRequest accountRequest);
    Task<DHTResponse<NodeInfo>> FindResponsibleNodeAsync(byte[] nodeHash);
    Task<DHTResponse<NodeInfo>> GetClosestNode(byte[] accountHash);
    Task<DHTResponse<NodeInfo>> FindNextClosestNodeAsync(byte[] accountHash, long currentDistance);
    Task<DHTResponse<bool>> NodeHasDataForKeyAsync(NodeInfo nodeInfo, byte[] accountHash);
    Task<DHTResponse<bool>> HasNextHop(NodeInfo currentNode, string targetHash);
    Task<DHTResponse<AccountResponse>> FetchAccountData(AccountRequest accountRequest);
    Task<DHTResponse<AccountResponse>> QueryBankAsync(string nodeBaseUrl, AccountRequest accountRequest);

}
