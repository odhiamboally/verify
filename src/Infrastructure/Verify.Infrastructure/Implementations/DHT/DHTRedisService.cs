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
using Verify.Application.Dtos.Bank;
using Verify.Application.Dtos.Common;
using Verify.Domain.Enums;

namespace Verify.Infrastructure.Implementations.DHT;
internal sealed class DHTRedisService : IDHTRedisService
{
    private readonly IDatabase redisDatabase;

    public DHTRedisService(IDatabase RedisDatabase)
    {
        redisDatabase = RedisDatabase;
            
    }


    public async Task<DHTResponse<bool>> NodeExists(byte[] bicHash)
    {
        try
        {
            return await redisDatabase.KeyExistsAsync(bicHash)
            ? DHTResponse<bool>.Success($"Node - {bicHash} - Exists", true)
            : DHTResponse<bool>.Failure($"Node - {bicHash} - does not exist", false);
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<DHTResponse<NodeInfo>> GetNodeDetails(byte[] bicHash)
    {
        try
        {
            // Retrieve node information from Redis
            var nodeInfo = await redisDatabase.StringGetAsync(bicHash);

            if (!nodeInfo.HasValue)
            {
                return DHTResponse<NodeInfo>.Failure($"Node {bicHash} not found", null);
            }

            var nodeDetails = JsonConvert.DeserializeObject<NodeInfo>(nodeInfo!);

            return DHTResponse<NodeInfo>.Success("Success", nodeDetails!);
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<DHTResponse<List<NodeInfo>>> GetAllNodesAsync()
    {
        try
        {
            List<NodeInfo> nodes = new();

            var allNodes = await redisDatabase.HashGetAllAsync("dht:nodes");
            if (allNodes.Length > 0)
            {
                foreach (var nodeEntry in allNodes)
                {
                    var nodeInfo = JsonConvert.DeserializeObject<NodeInfo>(nodeEntry.Value!);
                    nodes.Add(nodeInfo!);
                }
                return DHTResponse<List<NodeInfo>>.Success("Nodes retrieved successfully", nodes);
            }

            return DHTResponse<List<NodeInfo>>.Failure("No nodes found", new List<NodeInfo>());
        }
        catch (Exception)
        {
            throw;
        }
    }

    // Retrieve nodes in a sorted set based on score (minScore to maxScore)
    public async Task<DHTResponse<List<NodeInfo>>> GetNodesByScoreRangeAsync(string key, long minScore, long maxScore)
    {
        try
        {
            List<NodeInfo> nodes = new();

            // Fetch nodes based on rank range
            RedisValue[] nodeIds = await redisDatabase.SortedSetRangeByRankAsync(key, minScore, maxScore, Order.Ascending);
            if (nodeIds.Length == 0)
            {
                return DHTResponse<List<NodeInfo>>.Failure("No nodes found in the bucket.");
            }

            foreach (var nodeId in nodeIds)
            {
                // Fetch the node information from the Redis hash
                RedisValue serializedNodeInfo = await redisDatabase.HashGetAsync("dht:nodes", nodeId);
                //RedisValue serializedNodeInfo = await redisDatabase.StringGetAsync(nodeId.ToString());

                if (!serializedNodeInfo.IsNullOrEmpty)
                {
                    nodes.Add(JsonConvert.DeserializeObject<NodeInfo>(serializedNodeInfo!)!);
                }
            }

            if (nodes.Count > 0)
            {
                return DHTResponse<List<NodeInfo>>.Success("Nodes retrieved successfully", nodes);
            }

            return DHTResponse<List<NodeInfo>>.Failure("No nodes found in the bucket.");
        }
        catch (Exception)
        {
            throw;
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

    // Get the number of elements in a Redis sorted set (a bucket)
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

    // Retrieve the least recently seen node (assuming this is stored as the lowest-scoring node in a sorted set)
    public async Task<DHTResponse<string>> GetLeastRecentlySeenNodeHash(string key)
    {
        try
        {
            // Get the node with the smallest score (most likely the least recently seen node)
            RedisValue[] leastRecentlySeenNode = await redisDatabase.SortedSetRangeByRankAsync(key, 0, 0, Order.Ascending);
            if (leastRecentlySeenNode.Length > 0)
            {

                return DHTResponse<string>.Success("Least recently seen node retrieved", leastRecentlySeenNode[0]!);
            }

            return DHTResponse<string>.Failure("No nodes found in the bucket.");
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<DHTResponse<NodeInfo>> GetLeastRecentlySeenNode(string bucketKey, string nodeKey)
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

    public async Task<DHTResponse<NodeInfo>> GetNodeAsync(string key, byte[] field)
    {
        try
        {
            var nodeData = await redisDatabase.HashGetAsync(key, field);
            if (nodeData.IsNullOrEmpty)
            {
                return DHTResponse<NodeInfo>.Failure("Value not found.");
            }

            var node = JsonConvert.DeserializeObject<NodeInfo>(nodeData!);
            return DHTResponse<NodeInfo>.Success("Value retrieved successfully", node!);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task SetNodeAsync(string key, byte[] field, NodeInfo value, TimeSpan? expiry = null)
    {
        try
        {
            var serializedValue = JsonConvert.SerializeObject(value);
            await redisDatabase.HashSetAsync(key, field, serializedValue);

            if (expiry.HasValue)
            {
                await redisDatabase.KeyExpireAsync(key, expiry);
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    // Add a node to a sorted set with a specific score
    public async Task SetSortedNodeAsync(string bucketKey, string nodeKey, NodeInfo value, double score)
    {
        try
        {
            var serializedValue = JsonConvert.SerializeObject(value);
            await redisDatabase.SortedSetAddAsync(bucketKey, value.NodeHash, score);
            await redisDatabase.HashSetAsync(nodeKey, value.NodeHash, serializedValue);
            await redisDatabase.StringSetAsync(value.NodeHash, serializedValue, TimeSpan.FromHours(24));
        }
        catch (Exception)
        {
            throw;
        }
    }
    
    public async Task RemoveValueAsync(string key, string field)
    {
        try
        {
            await redisDatabase.HashDeleteAsync(key, field);
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
            // Use a Redis transaction to update the data atomically
            var transaction = redisDatabase.CreateTransaction();

            // Watch the key to ensure the transaction only succeeds if the key hasn't changed
            transaction.AddCondition(Condition.KeyExists(bicHash));

            // Queue the update operation in the transaction (update node info in the hash)
            _ = transaction.HashSetAsync("dht:nodes", bicHash, JsonConvert.SerializeObject(nodeInfo));

            if (expiry.HasValue)
            {
                await redisDatabase.KeyExpireAsync("dht:nodes", expiry);
            }

            // Execute the transaction atomically
            if (!await transaction.ExecuteAsync())
            {
                return DHTResponse<bool>.Success(
                "Update successful",
                false);
            }

            return DHTResponse<bool>.Success(
                "Update successful",
                true);
        }
        catch (Exception)
        {

            throw;
        }
    }





}
