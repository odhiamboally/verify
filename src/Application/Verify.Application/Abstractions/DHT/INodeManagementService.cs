using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verify.Application.Dtos.Bank;
using Verify.Application.Dtos.Common;
using Verify.Domain.Enums;

namespace Verify.Application.Abstractions.DHT;
public interface INodeManagementService
{

    Task<DHTResponse<NodeInfo>> RegisterOrUpdateNodeAsync(AddNodeRequest nodeRequest);
    Task<DHTResponse<NodeInfo>> GetNodeDetails(byte[] bicHash);
    Task<DHTResponse<string>> GetNodeEndpointAsync(byte[] accountHash);
    Task<DHTResponse<string>> GetNodeEndpointFromConfigAsync(byte[] bicHash);
    Task<DHTResponse<List<NodeInfo>>> GetAllNodesAsync();
    Task<DHTResponse<List<NodeInfo>>> GetActiveNodesInBucketAsync(int distance);
    Task<DHTResponse<NodeInfo>> AddOrUpdateNodeInDHTAsync(NodeInfo nodeInfo);
    Task<DHTResponse<NodeInfo>> AddOrUpdateNodeAsync(NodeInfo nodeInfo, StorageType storageType, bool applyEviction);
    //Task<DHTResponse<NodeInfo>> AddOrUpdateNodeAsync(NodeInfo nodeInfo, bool withEviction = true);
    Task<DHTResponse<bool>> AddOrUpdateNodeAsync(NodeInfo nodeInfo, bool withEviction = true);
    Task<DHTResponse<bool>> BroadcastNodePresenceAsync(byte[] bicHash, NodeInfo nodeInfo);
    Task<DHTResponse<bool>> PingNodeAsync(NodeInfo node);



}
