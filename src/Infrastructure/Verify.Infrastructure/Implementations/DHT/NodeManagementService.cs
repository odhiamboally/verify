using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Polly;

using Quartz.Util;

using Refit;

using StackExchange.Redis;

using Verify.Application.Abstractions.DHT;
using Verify.Application.Dtos.Bank;
using Verify.Application.Dtos.Common;
using Verify.Domain.Enums;
using Verify.Infrastructure.Utilities.DHT;
using Verify.Shared.Exceptions;

namespace Verify.Infrastructure.Implementations.DHT;
internal sealed class NodeManagementService : INodeManagementService
{
    private readonly HttpClient httpClient;
    private readonly IHashingService hashingService;
    private readonly IDHTRedisService dhtRedisService;
    private readonly IConfiguration configuration;
    private const int BucketSize = 20;


    public NodeManagementService(
        IHttpClientFactory httpClientFactory,
        IHashingService HashingService, 
        IDHTRedisService DHTRedisService,
        IConfiguration Configuration)
    {
        httpClient = httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(100);
        hashingService = HashingService;
        dhtRedisService = DHTRedisService;
        configuration = Configuration;
        
    }


    public async Task<DHTResponse<NodeInfo>> RegisterOrUpdateNodeAsync(AddNodeRequest addNodeRequest)
    {
        try
        {
            var nodeHashResponse = await hashingService.ByteHash(addNodeRequest.BankBIC);
            addNodeRequest = new AddNodeRequest
            {
                BankBIC = addNodeRequest.BankBIC,
                BankUri = addNodeRequest.BankUri,
                BankHash = nodeHashResponse.Data!,
                BankEndPoint = addNodeRequest.BankEndPoint,

            };

            var nodeExists = await dhtRedisService.NodeExistsAsync("dht:nodes", nodeHashResponse.Data!);
            if (nodeExists.Data)
            {
                // Update node's information
                await UpdateNodeInDHTAsync(addNodeRequest, nodeHashResponse.Data!);
                return DHTResponse<NodeInfo>.Success("Node updated successfully", new NodeInfo
                {
                    NodeBIC = addNodeRequest.BankBIC,
                    NodeUri = addNodeRequest.BankUri,
                    NodeHash = nodeHashResponse.Data ?? Array.Empty<byte>(),
                    NodeEndPoint = string.Empty,
                });
            }
            else
            {
                // Register new node
                await RegisterNodeInDHTAsync(addNodeRequest, nodeHashResponse.Data!);
                return DHTResponse<NodeInfo>.Success("Node registered successfully", new NodeInfo
                {
                    NodeBIC = addNodeRequest.BankBIC,
                    NodeUri = addNodeRequest.BankUri,
                    NodeHash = nodeHashResponse.Data ?? Array.Empty<byte>(),
                    NodeEndPoint = string.Empty,
                });
            }

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
            return await dhtRedisService.GetNodeAsync("dht:nodes", bicHash);
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<DHTResponse<string>> GetNodeEndpointAsync(byte[] accountHash)
    {
        try
        {
            // Use the hashed account to retrieve the bank node info from Redis
            var dhtResponse = await dhtRedisService.GetNodeAsync("dht:nodes", accountHash);

            if (dhtResponse.Successful)
            {
                return DHTResponse<string>.Success(
                    "",
                    dhtResponse.Data!.NodeEndPoint!);
            }

            return DHTResponse<string>.Success(
                    "",
                    string.Empty);
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<DHTResponse<string>> GetNodeEndpointFromConfigAsync(byte[] bicHash)
    {
        try
        {
            var bicHashString = Convert.ToBase64String(bicHash);
            var nodeEnpoint = configuration[$"NodeConfig:{bicHashString}"];
            if (!String.IsNullOrWhiteSpace(nodeEnpoint))
            {
                return DHTResponse<string>.Success("Success", nodeEnpoint!);
            }

            return DHTResponse<string>.Success("Failed", string.Empty);
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
            return await dhtRedisService.GetAllNodesAsync("dht:nodes");

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
            return await dhtRedisService.GetActiveNodesInBucketAsync(distance);
        }
        catch (Exception)
        {

            throw;
        }       
    }

    public async Task<DHTResponse<NodeInfo>> AddOrUpdateNodeInDHTAsync(NodeInfo nodeInfo)
    {
        try
        {
            // Calculate XOR distance (we need to determine nodeHash somehow)
            var currentNodeHash = await hashingService.ByteHash(nodeInfo.NodeBIC);
            int distance = DHTUtilities.CalculateXorDistance(currentNodeHash.Data!, nodeInfo.NodeHash);
            string redisBucketsKey = $"dht:buckets:{distance}";
            string redisNodesKey = $"dht:nodes";

            // Add or update node in Redis
            var existingNodeJson = await dhtRedisService.GetNodeAsync(redisNodesKey, nodeInfo.NodeHash);

            if (existingNodeJson.Data != null)
            {
                var existingNode = new NodeInfo
                {
                    NodeBIC = nodeInfo.NodeBIC,
                    NodeUri = nodeInfo.NodeUri,
                    NodeHash = nodeInfo.NodeHash,
                    NodeEndPoint = nodeInfo.NodeEndPoint,
                    KnownPeers = nodeInfo.KnownPeers,
                    LastSeen = DateTimeOffset.UtcNow,

                };

                // Update in Redis
                await dhtRedisService.SetNodeAsync(redisNodesKey, nodeInfo.NodeHash, JsonConvert.SerializeObject(existingNode), TimeSpan.FromHours(24));
                return DHTResponse<NodeInfo>.Success("Node updated in DHT", existingNode);
            }
            else
            {
                // Node does not exist, check bucket size
                var bucketCount = await dhtRedisService.GetBucketCountAsync(redisBucketsKey, StorageType.Redis);

                if (bucketCount.Data < BucketSize)
                {
                    // Bucket has space, add new node
                    await dhtRedisService.SetSortedNodeAsync(redisBucketsKey, redisNodesKey, nodeInfo, distance);
                    return DHTResponse<NodeInfo>.Success("Node added to DHT", nodeInfo);
                }
                else
                {
                    // Bucket is full, implement eviction logic
                    var leastRecentlySeenNode = await dhtRedisService.GetLeastRecentlySeenNodeAsync(redisBucketsKey, redisNodesKey);

                    // Check reachability (example: ping)
                    var isReachable = await PingNodeAsync(leastRecentlySeenNode.Data!);
                    if (!isReachable.Data)
                    {
                        // Replace least recently seen node
                        await dhtRedisService.RemoveValueAsync(redisNodesKey, leastRecentlySeenNode.Data!.NodeHash);
                        await dhtRedisService.SetSortedNodeAsync(redisBucketsKey, redisNodesKey, nodeInfo, distance);

                        return DHTResponse<NodeInfo>.Success("Replaced least recently seen node in DHT", nodeInfo);
                    }
                    else
                    {
                        // If all nodes are reachable, return failure
                        return DHTResponse<NodeInfo>.Failure("Bucket is full, and all nodes are reachable.");
                    }
                }
            }
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<DHTResponse<NodeInfo>> AddOrUpdateNodeAsync(NodeInfo nodeInfo, StorageType storageType, bool applyEviction)
    {
        try
        {
            // Step 1: Calculate the node hash (shared by all implementations)
            var currentNodeHash = await hashingService.ByteHash(nodeInfo.NodeBIC);
            var xorDistance = DHTUtilities.CalculateXorDistance(currentNodeHash.Data!, nodeInfo.NodeHash);
            var redisBucketsKey = $"dht:buckets:{xorDistance}";
            var redisNodesKey = $"dht:nodes";

            // Step 2: Check if the node exists (for both Redis and in-memory)
            var existingNodeResponse = storageType == StorageType.Redis
                ? await dhtRedisService.GetNodeAsync(redisNodesKey, nodeInfo.NodeHash)
                : null;

            if (existingNodeResponse?.Data != null)
            {
                // Node exists, update it
                var updatedNode = UpdateNode(existingNodeResponse.Data!, nodeInfo);
                await SaveNodeAsync(redisNodesKey, updatedNode, storageType);
                return DHTResponse<NodeInfo>.Success("Node updated successfully", updatedNode);
            }
            else
            {
                // Step 3: Check bucket size and apply eviction if necessary
                var bucketCount = await dhtRedisService.GetBucketCountAsync(redisBucketsKey, storageType);
                if (bucketCount.Data < BucketSize || !applyEviction)
                {
                    // Bucket has space or eviction is not required, add new node
                    await SaveNodeAsync(redisNodesKey, nodeInfo, storageType);
                    return DHTResponse<NodeInfo>.Success("Node added successfully", nodeInfo);
                }
                else
                {
                    // Step 4: Apply eviction logic (if bucket is full and eviction is enabled)
                    var evictionResult = await ApplyEvictionPolicyAsync(redisBucketsKey, redisNodesKey, nodeInfo, storageType);
                    if (evictionResult.Successful)
                    {
                        return DHTResponse<NodeInfo>.Success(evictionResult.Message!, nodeInfo);
                    }
                    else
                    {
                        return DHTResponse<NodeInfo>.Failure(evictionResult.Message!);
                    }
                }
            }
        }
        catch (Exception)
        {
            // Log exception
            throw;
        }
    }

    public async Task<DHTResponse<bool>> AddOrUpdateNodeAsync(NodeInfo nodeInfo, bool withEviction = true)
    {
        try
        {
            // Calculate XOR distance to determine the bucket
            var currentNodeHash = await hashingService.ByteHash(nodeInfo.NodeBIC);
            int distance = DHTUtilities.CalculateXorDistance(currentNodeHash.Data!, nodeInfo.NodeHash);
            string redisBucketsKey = $"dht:buckets:{distance}";
            string redisNodesKey = $"dht:nodes";

            // Fetch existing node from Redis or in-memory
            var existingNodeResponse = await dhtRedisService.GetNodeAsync(redisNodesKey, nodeInfo.NodeHash);
            if (existingNodeResponse?.Data != null)
            {
                // Node exists, update its info
                var updatedNode = UpdateNodeInfo(existingNodeResponse.Data!, nodeInfo);
                await dhtRedisService.SetNodeAsync(redisNodesKey, nodeInfo.NodeHash, JsonConvert.SerializeObject(updatedNode), TimeSpan.FromHours(24));
                return DHTResponse<bool>.Success("Node updated successfully", true, null, new Dictionary<string, object>() { { "node", updatedNode } });
            }
            else
            {
                return await HandleNodeAdditionWithEvictionAsync(redisBucketsKey, redisNodesKey, nodeInfo, distance, withEviction);
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<DHTResponse<bool>> BroadcastNodePresenceAsync(byte[] bankHash, NodeInfo addToRoutingTableRequest)
    {
        try
        {
            // Broadcast the new bank's presence to its known peers
            foreach (var peerUri in addToRoutingTableRequest.KnownPeers!)
            {
                using (httpClient)
                {
                    var content = new StringContent(JsonConvert.SerializeObject(addToRoutingTableRequest), Encoding.UTF8, "application/json");

                    await httpClient.PostAsync($"{peerUri}/AnnouncePresence", content);
                }
            }

            return DHTResponse<bool>.Success(
                "Success",
                true
                );
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<DHTResponse<bool>> PingNodeAsync(NodeInfo nodeInfo)
    {
        var pingUri = new Uri(nodeInfo.NodeUri, "ping");

        // Define the retry policy with exponential backoff
        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>() // Retry for both network errors and timeouts
            .WaitAndRetryAsync(
                retryCount: 3, // Retry 3 times
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)) // Exponential backoff: 2s, 4s, 8s
            );

        try
        {
            // Execute the ping with retry policy
            var result = await retryPolicy.ExecuteAsync(async () =>
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
                {
                    var response = await httpClient.GetAsync(pingUri, cts.Token);
                    return response.IsSuccessStatusCode;
                }
            });

            return DHTResponse<bool>.Success(
                "",
                result);
        }
        catch (TaskCanceledException)
        {
            // This exception indicates a timeout (node is unreachable)
            throw;
        }
        catch (HttpRequestException)
        {
            // This exception indicates a network-level error (e.g., connection refused)
            throw;
        }
        catch (Exception)
        {
            throw;
        }
    }



    #region Private Methods

    private async Task RegisterNodeInDHTAsync(AddNodeRequest addNodeRequest, byte[] nodeHash)
    {
        try
        {
            var nodeInfo = new NodeInfo
            {
                NodeBIC = addNodeRequest.BankBIC,
                NodeHash = addNodeRequest.BankHash,
                NodeUri = addNodeRequest.BankUri,
                NodeEndPoint = addNodeRequest.BankEndPoint,
                LastSeen = DateTimeOffset.UtcNow
            };

            // Store node info in Redis
            await dhtRedisService.SetNodeAsync("dht:buckets", nodeHash, JsonConvert.SerializeObject(nodeInfo), TimeSpan.FromHours(24));

        }
        catch (Exception)
        {

            throw;
        }
    }

    private async Task SaveNodeAsync(string redisNodeKey, NodeInfo nodeInfo, StorageType storageType)
    {
        if (storageType == StorageType.Redis)
        {
            await dhtRedisService.SetNodeAsync(redisNodeKey, nodeInfo.NodeHash, JsonConvert.SerializeObject(nodeInfo), TimeSpan.FromHours(24));
        }
        else
        {
            // Save node in memory bucket

        }
    }

    private NodeInfo UpdateNode(NodeInfo existingNode, NodeInfo newNodeInfo)
    {
        return new NodeInfo
        {
            NodeBIC = newNodeInfo.NodeBIC,
            NodeUri = newNodeInfo.NodeUri,
            NodeHash = newNodeInfo.NodeHash,
            NodeEndPoint = newNodeInfo.NodeEndPoint,
            LastSeen = DateTimeOffset.UtcNow,
            KnownPeers = newNodeInfo.KnownPeers ?? existingNode.KnownPeers
        };
    }

    private NodeInfo UpdateNodeInfo(NodeInfo existingNode, NodeInfo newNode)
    {
        return new NodeInfo
        {
            NodeBIC = newNode.NodeBIC,
            NodeUri = newNode.NodeUri,
            NodeHash = newNode.NodeHash,
            NodeEndPoint = newNode.NodeEndPoint,
            LastSeen = DateTimeOffset.UtcNow,
            KnownPeers = newNode.KnownPeers ?? existingNode.KnownPeers
        };
    }

    private async Task UpdateNodeInDHTAsync(AddNodeRequest nodeRequest, byte[] nodeHash)
    {
        try
        {
            var nodeInfo = new NodeInfo
            { 
                NodeBIC = nodeRequest.BankBIC,
                NodeHash = nodeRequest.BankHash,
                NodeUri= nodeRequest.BankUri,
                NodeEndPoint= nodeRequest.BankEndPoint,
                LastSeen = DateTimeOffset.UtcNow
            };

            await dhtRedisService.UpdateUsingTransaction(nodeHash, nodeInfo, TimeSpan.FromHours(24));

        }
        catch (Exception)
        {

            throw;
        }
    }

    private async Task<DHTResponse<string>> ApplyEvictionPolicyAsync(string redisBucketsKey, string redisNodesKey, NodeInfo newNode, StorageType storageType)
    {
        try
        {
            var leastRecentlySeenNode = storageType == StorageType.Redis
                ? await dhtRedisService.GetLeastRecentlySeenNodeAsync(redisBucketsKey, redisNodesKey)
                : DHTResponse<NodeInfo>.Success("", null!);

            // Check reachability
            var isReachable = await PingNodeAsync(leastRecentlySeenNode.Data!);
            if (!isReachable.Data)
            {
                // Replace the unreachable node
                if (storageType == StorageType.Redis)
                {
                    await dhtRedisService.RemoveValueAsync(redisNodesKey, leastRecentlySeenNode.Data!.NodeHash!);
                }

                //RemoveNodeFromMemoryBucket(redisKey, leastRecentlySeenNode.Data!);

                await SaveNodeAsync(redisNodesKey, newNode, storageType);

                return DHTResponse<string>.Success("Replaced unreachable node", string.Empty);
            }
            return DHTResponse<string>.Failure("Bucket is full, and all nodes are reachable");
        }
        catch (Exception)
        {

            throw;
        }
        
    }

    private async Task<DHTResponse<bool>> HandleNodeAdditionWithEvictionAsync(string redisBucketsKey, string redisNodesKey, NodeInfo nodeInfo, int distance, bool withEviction)
    {
        // Check bucket size for the XOR distance bucket
        var bucketCount = await dhtRedisService.GetBucketCountAsync(redisBucketsKey, StorageType.Redis);
        if (bucketCount.Data < BucketSize)
        {
            // Bucket has space, add the new node
            return await dhtRedisService.SetSortedNodeAsync(redisBucketsKey, redisNodesKey, nodeInfo, distance);
                
        }
        else if (withEviction)
        {
            // Handle eviction if the bucket is full
            return await EvictAndReplaceNode(redisBucketsKey, redisNodesKey, nodeInfo, distance);
        }
        else
        {
            return DHTResponse<bool>.Failure("Bucket is full, and eviction is disabled.");
        }
    }

    private async Task<DHTResponse<bool>> EvictAndReplaceNode(string redisBucketsKey, string redisNodesKey, NodeInfo newNode, int distance)
    {
        // Get least recently seen node for eviction
        var leastRecentlySeenNode = await dhtRedisService.GetLeastRecentlySeenNodeAsync(redisBucketsKey, redisNodesKey);

        // Check reachability (ping) for eviction,
        var isReachable = await PingNodeAsync(leastRecentlySeenNode.Data!);
        if (!isReachable.Data)
        {
            // Replace least recently seen node
            await dhtRedisService.RemoveValueAsync(redisNodesKey, leastRecentlySeenNode.Data.NodeHash);
            await dhtRedisService.SetSortedNodeAsync(redisBucketsKey, redisNodesKey, newNode, distance);
            return DHTResponse<bool>.Success("Replaced least recently seen node", true, null, new Dictionary<string, object>() { { "node", newNode } });
        }
        else
        {
            return DHTResponse<bool>.Failure("Bucket is full, and all nodes are reachable.");
        }
    }

    private bool ShouldReplaceNode(NodeInfo leastRecentlySeenNode, NodeInfo newNode)
    {
        // Customize your eviction policy here, e.g., based on age, frequency, or node metrics
        // For example, we'll replace if the new node has been seen more recently than the least recently seen node
        return leastRecentlySeenNode.LastSeen < newNode.LastSeen;
    }

    private void QueueRejectedNodeForFuture(NodeInfo rejectedNode)
    {
        // Implement a queue to reattempt adding the rejected node later
        // This could be a Redis queue, a memory cache, or a database table for retries
        // For demonstration, we will simulate a queue using a ConcurrentQueue
        var rejectedNodesQueue = new ConcurrentQueue<NodeInfo>();
        rejectedNodesQueue.Enqueue(rejectedNode);
    }

    

    #endregion



}
