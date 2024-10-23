using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using StackExchange.Redis;

using Verify.Application.Dtos.Account;
using Verify.Application.Dtos.Bank;
using Verify.Application.Dtos.Common;
using Verify.Domain.Enums;

namespace Verify.Application.Abstractions.DHT;
public interface IDHTRedisService
{
    Task<DHTResponse<bool>> NodeExistsAsync(string key, byte[] hash);
    Task<DHTResponse<List<NodeInfo>>> GetAllNodesAsync(string key);
    Task<DHTResponse<List<NodeInfo>>> GetNodesByScoreRangeAsync(string key, long minRank, long maxRank); // Retrieve nodes based on score
    Task<DHTResponse<List<NodeInfo>>> GetActiveNodesInBucketAsync(int distance);
    Task<DHTResponse<long>> GetBucketCountAsync(string key, StorageType storageType);
    Task<DHTResponse<NodeInfo>> GetLeastRecentlySeenNodeAsync(string bucketKey, string nodeKey);
    Task<DHTResponse<AccountInfo>> GetAccountNodeAsync(string key, byte[] field);
    Task<DHTResponse<NodeInfo>> GetNodeAsync(string key, byte[] field);
    Task<DHTResponse<bool>> SetNodeAsync(string key, byte[] field, string serializedValue, TimeSpan? expiry = null);

    // Sorted set to store nodes by distance or other criteria
    Task<DHTResponse<bool>> SetSortedNodeAsync(string bucketKey, string nodeKey, NodeInfo value, double score); 
    Task<DHTResponse<bool>> SetSortedAccountAsync(string bucketKey, string accountKey, AccountInfo value, double score);
    Task<DHTResponse<bool>> RemoveValueAsync(string key, byte[] field);
    Task<DHTResponse<bool>> UpdateUsingTransaction(byte[] bicHash, NodeInfo nodeInfo, TimeSpan? expiry = null);


    
}
