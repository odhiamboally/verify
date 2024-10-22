using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verify.Application.Dtos.Bank;
using Verify.Application.Dtos.Common;
using Verify.Domain.Enums;

namespace Verify.Application.Abstractions.DHT;
public interface IDHTRedisService
{
    Task<DHTResponse<bool>> NodeExists(byte[] bicHash);
    Task<DHTResponse<NodeInfo>> GetNodeDetails(byte[] bicHash);
    Task<DHTResponse<List<NodeInfo>>> GetAllNodesAsync();
    Task<DHTResponse<List<NodeInfo>>> GetNodesByScoreRangeAsync(string key, long minRank, long maxRank); // Retrieve nodes based on score
    Task<DHTResponse<List<NodeInfo>>> GetActiveNodesInBucketAsync(int distance);
    Task<DHTResponse<long>> GetBucketCountAsync(string key, StorageType storageType);
    Task<DHTResponse<string>> GetLeastRecentlySeenNodeHash(string key);
    Task<DHTResponse<NodeInfo>> GetLeastRecentlySeenNode(string bucketKey, string nodeKey);
    Task<DHTResponse<NodeInfo>> GetNodeAsync(string key, byte[] field);
    Task SetNodeAsync(string key, byte[] field, NodeInfo value, TimeSpan? expiry = null);
    Task SetSortedNodeAsync(string bucketKey, string nodeKey, NodeInfo value, double score); // Sorted set to store nodes by distance or other criteria

    Task RemoveValueAsync(string key, string field);
    Task<DHTResponse<bool>> UpdateUsingTransaction(byte[] bicHash, NodeInfo nodeInfo, TimeSpan? expiry = null);
}
