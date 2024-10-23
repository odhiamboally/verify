using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using StackExchange.Redis;

using Verify.Application.Abstractions.DHT;
using Verify.Application.Dtos.Account;
using Verify.Application.Dtos.Bank;
using Verify.Application.Dtos.Common;
using Verify.Domain.Enums;

using static MassTransit.ValidationResultExtensions;

namespace Verify.Infrastructure.Implementations.DHT;
internal sealed class DHTRedisService : IDHTRedisService
{
    private readonly IDatabase redisDatabase;

    public DHTRedisService(IDatabase RedisDatabase)
    {
        redisDatabase = RedisDatabase;
            
    }


    public async Task<DHTResponse<bool>> NodeExistsAsync(string key, byte[] hash)
    {
        try
        {
            bool exists = await redisDatabase.HashExistsAsync(key, hash);
            string message = exists
                ? $"Node {Convert.ToBase64String(hash)} exists"
                : $"Node {Convert.ToBase64String(hash)} does not exist";

            return exists
                ? DHTResponse<bool>.Success(message, true)
                : DHTResponse<bool>.Failure(message, false);
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error checking node existence: {ex.Message}", ex);
        }
    }

    public async Task<DHTResponse<NodeInfo>> GetNodeAsync(string key, byte[] field)
    {
        try
        {
            var nodeData = await redisDatabase.HashGetAsync(key, field);
            if (nodeData.IsNullOrEmpty)
            {
                return DHTResponse<NodeInfo>.Failure("Node not found.");
            }

            var node = JsonConvert.DeserializeObject<NodeInfo>(nodeData!);
            return DHTResponse<NodeInfo>.Success("Node retrieved successfully", node!);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<DHTResponse<AccountInfo>> GetAccountNodeAsync(string key, byte[] accountHash)
    {
        try
        {
            var nodeData = await redisDatabase.HashGetAsync(key, accountHash);
            if (nodeData.IsNullOrEmpty)
            {
                return DHTResponse<AccountInfo>.Failure("Node not found.");
            }

            var node = JsonConvert.DeserializeObject<AccountInfo>(nodeData!);
            return DHTResponse<AccountInfo>.Success("Node retrieved successfully", node!);
        }
        catch (Exception)
        {
            throw;
        }
    }


    public async Task<DHTResponse<List<NodeInfo>>> GetAllNodesAsync(string key)
    {
        try
        {
            List<NodeInfo> nodes = new();

            var allNodes = await redisDatabase.HashGetAllAsync(key);
            foreach (var nodeEntry in allNodes)
            {
                nodes.Add(DeserializeNodeInfo(nodeEntry.Value));
            }

            return nodes.Any()
                ? DHTResponse<List<NodeInfo>>.Success("Nodes retrieved successfully", nodes)
                : DHTResponse<List<NodeInfo>>.Failure("No nodes found", new List<NodeInfo>());
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error retrieving all nodes: {ex.Message}", ex);
        }
    }

    // Retrieve nodes in a sorted set based on score (minScore to maxScore)
    public async Task<DHTResponse<List<NodeInfo>>> GetNodesByScoreRangeAsync(string key, long minScore, long maxScore)
    {
        try
        {
            List<NodeInfo> nodes = new();
            var nodeIds = await redisDatabase.SortedSetRangeByRankAsync(key, minScore, maxScore);
            foreach (var nodeId in nodeIds)
            {
                var serializedNodeInfo = await redisDatabase.HashGetAsync(key, nodeId);
                if (!serializedNodeInfo.IsNullOrEmpty)
                {
                    nodes.Add(DeserializeNodeInfo(serializedNodeInfo));
                }
            }

            return nodes.Any()
                 ? DHTResponse<List<NodeInfo>>.Success("Nodes retrieved successfully", nodes)
                 : DHTResponse<List<NodeInfo>>.Failure("No nodes found in the range", null);
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error retrieving nodes by score range: {ex.Message}", ex);
        }
    }

    public async Task<DHTResponse<List<NodeInfo>>> GetActiveNodesInBucketAsync(int distance)
    {
        try
        {
            string bucketKey = $"bucket:{distance}";

            // Retrieve active nodes from the sorted set
            var nodesBICs = await redisDatabase.SortedSetRangeByScoreAsync(bucketKey, start: 0, stop: DateTime.UtcNow.Ticks);
            var nodes = new List<NodeInfo>();

            foreach (var nodeBIC in nodesBICs)
            {
                var jsonData = await redisDatabase.StringGetAsync(nodeBIC.ToString());
                if (!jsonData.IsNull)
                {
                    nodes.Add(JsonConvert.DeserializeObject<NodeInfo>(jsonData!)!);
                }
            }

            return DHTResponse<List<NodeInfo>>.Success(
                "Success",
                nodes);
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<DHTResponse<long>> GetBucketCountAsync(string key, StorageType storageType)
    {
        try
        {
            if (storageType == StorageType.Redis)
            {
                long count = await redisDatabase.SortedSetLengthAsync(key);
                return DHTResponse<long>.Success("Bucket count retrieved successfully", count);
            }

            //ToDo: Get Count From InMemeory
            return DHTResponse<long>.Success("Bucket count retrieved successfully", 0);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<DHTResponse<NodeInfo>> GetLeastRecentlySeenNodeAsync(string bucketKey, string nodeKey)
    {
        try
        {
            // Get the node with the smallest score (most likely the least recently seen node)
            var leastRecentlySeenNodeHash = await GetLeastRecentlySeenNodeHash(bucketKey);
            if (leastRecentlySeenNodeHash.Data!.Length > 0)
            {

                // Step 2: Use the node hash (field) to retrieve the actual NodeInfo object
                RedisValue serializedNodeInfo = await redisDatabase.HashGetAsync(nodeKey, leastRecentlySeenNodeHash.Data);

                if (!serializedNodeInfo.IsNullOrEmpty)
                {
                    // Deserialize the stored value back into a NodeInfo object
                    var nodeInfo = JsonConvert.DeserializeObject<NodeInfo>(serializedNodeInfo!);

                    return DHTResponse<NodeInfo>.Success("Least recently seen node retrieved", nodeInfo!);
                }

                return DHTResponse<NodeInfo>.Failure("Node info not found in the hash.");
            }

            return DHTResponse<NodeInfo>.Failure("No nodes found in the bucket.");
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<DHTResponse<bool>> SetNodeAsync(string key, byte[] field, string serializedValue, TimeSpan? expiry = null)
    {
        try
        {
            //var serializedValue = SerializeNodeInfo(value);
            await redisDatabase.HashSetAsync(key, field, serializedValue);
            if (expiry.HasValue)
            {
                await redisDatabase.KeyExpireAsync(key, expiry);
            }

            return DHTResponse<bool>.Success("Node added/updated successfully", true);
        }
        catch (Exception)
        {
            throw;
        }
    }

    // Add a node to a sorted set with a specific score
    public async Task<DHTResponse<bool>> SetSortedNodeAsync(string bucketKey, string nodeKey, NodeInfo value, double score)
    {
        try
        {
            var serializedValue = SerializeNodeInfo(value);
            await redisDatabase.SortedSetAddAsync(bucketKey, value.NodeHash, score);
            await redisDatabase.HashSetAsync(nodeKey, value.NodeHash, serializedValue);
            await redisDatabase.StringSetAsync(value.NodeHash, serializedValue, TimeSpan.FromHours(24));

            return await redisDatabase.SortedSetAddAsync(bucketKey, value.NodeHash, score)
                ? DHTResponse<bool>.Success("Node added to DHT", true, null, new Dictionary<string, object>() { { "node", value } })
                : DHTResponse<bool>.Success("Failed to Add Node to DHT", true, null, null);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<DHTResponse<bool>> SetSortedAccountAsync(string bucketKey, string accountKey, AccountInfo value, double score)
    {
        try
        {
            var serializedValue = SerializeAccountInfo(value);
            await redisDatabase.SortedSetAddAsync(bucketKey, value.AccountHash, score);
            await redisDatabase.HashSetAsync(accountKey, value.AccountHash, serializedValue);
            await redisDatabase.StringSetAsync(value.AccountHash, serializedValue, TimeSpan.FromHours(24));

            return await redisDatabase.SortedSetAddAsync(bucketKey, value.AccountHash, score)
                ? DHTResponse<bool>.Success("Account added to DHT", true, null, new Dictionary<string, object>() { { "account", value } })
                : DHTResponse<bool>.Success("Failed to Add Account to DHT", true, null, null);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<DHTResponse<bool>> RemoveValueAsync(string key, byte[] field)
    {
        try
        {
            return await redisDatabase.HashDeleteAsync(key, field)
            ? DHTResponse<bool>.Success("Node removed successfully", true)
            : DHTResponse<bool>.Failure("Node not found", false);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<DHTResponse<bool>> UpdateUsingTransaction(byte[] bicHash, NodeInfo nodeInfo, TimeSpan? expiry = null)
    {
        try
        {
            var transaction = redisDatabase.CreateTransaction();

            // Watch the key to ensure the transaction only succeeds if the key hasn't changed
            transaction.AddCondition(Condition.KeyExists(bicHash));

            // Queue the update operation in the transaction (update node info in the hash)
            _ = transaction.HashSetAsync("dht:nodes", bicHash, SerializeNodeInfo(nodeInfo));

            if (expiry.HasValue)
            {
                await redisDatabase.KeyExpireAsync("dht:nodes", expiry);
            }

            return await transaction.ExecuteAsync()
                ? DHTResponse<bool>.Success("Update successful", true)
                : DHTResponse<bool>.Failure("Update failed", false);

        }
        catch (Exception)
        {

            throw;
        }
    }


    private async Task<DHTResponse<string>> GetLeastRecentlySeenNodeHash(string bucketKey)
    {
        try
        {
            var leastRecentlySeenNode = await redisDatabase.SortedSetRangeByRankAsync(bucketKey, 0, 0);
            if (leastRecentlySeenNode.Length > 0)
            {
                return DHTResponse<string>.Success("Least recently seen node retrieved", leastRecentlySeenNode[0]);
            }
            return DHTResponse<string>.Failure("No nodes found in the bucket.", null);
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error retrieving least recently seen node hash: {ex.Message}", ex);
        }
    }

    private NodeInfo DeserializeNodeInfo(RedisValue serializedNodeInfo)
    {
        return JsonConvert.DeserializeObject<NodeInfo>(serializedNodeInfo)!;
    }

    private AccountInfo DeserializeAccountInfo(RedisValue serializedAccountInfo)
    {
        return JsonConvert.DeserializeObject<AccountInfo>(serializedAccountInfo)!;
    }

    private string SerializeNodeInfo(NodeInfo nodeInfo)
    {
        return JsonConvert.SerializeObject(nodeInfo);
    }

    private string SerializeAccountInfo(AccountInfo accountInfo)
    {
        return JsonConvert.SerializeObject(accountInfo);
    }



}
