using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using StackExchange.Redis;

using Verify.Application.Abstractions.DHT;
using Verify.Application.Abstractions.Interfaces;
using Verify.Application.Dtos.Account;
using Verify.Application.Dtos.Bank;
using Verify.Application.Dtos.Common;
using Verify.Infrastructure.Utilities.DHT;
using Verify.Infrastructure.Utilities.DHT.ApiClients;
using Verify.Shared.Exceptions;

namespace Verify.Infrastructure.Implementations.DHT;
internal sealed class DHTService : IDHTService
{
    private readonly HttpClient httpClient;
    private readonly IApiClientFactory apiClientFactory;
    private readonly IHashingService hashingService;
    private readonly IReplicationService replicationService;
    private readonly INodeManagementService nodeManagementService;
    private readonly IDatabase redisDatabase;
    private readonly IDHTRedisService dHTRedisService;


    public DHTService(
        IHttpClientFactory httpClientFactory, 
        IApiClientFactory ApiClientFactory, 
        IHashingService HashingService,
        IReplicationService ReplicationService,
        INodeManagementService NodeManagementService,
        IDatabase RedisDatabase,
        IDHTRedisService DHTRedisService)
    {
        httpClient = httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(100);
        apiClientFactory = ApiClientFactory;
        hashingService = HashingService;
        replicationService = ReplicationService;
        nodeManagementService = NodeManagementService;
        redisDatabase = RedisDatabase;
        dHTRedisService = DHTRedisService;

    }


    public async Task<DHTResponse<AccountResponse>> FetchAccountData(AccountRequest accountRequest)
    {
        try
        {
            var accountResponse = await LookupAccountInMemoryAsync(accountRequest);
            if (accountResponse.Successful)
            {
                return accountResponse;
            }

            // Route the request using Kademlia's routing algorithm to find the responsible node
            var accountHash = await hashingService.ByteHash(accountRequest.RecipientAccountNumber);
            var responsibleNodeResponse = await FindResponsibleNodeAsync(accountHash.Data!);
            if (!responsibleNodeResponse.Successful)
            {
                return DHTResponse<AccountResponse>.Failure(responsibleNodeResponse.Message!);
            }

            var responsibleNode = responsibleNodeResponse.Data;

            var nodeEndPoint = await nodeManagementService.GetNodeEndpointAsync(accountHash.Data!);
            var accountDataResponse = await QueryBankAsync(nodeEndPoint.Data!, accountRequest);
            if (!accountDataResponse.Successful)
            {
                return DHTResponse<AccountResponse>.Failure("Failed to retrieve account details from the responsible node.");
            }

            var accountData = accountDataResponse.Data;

            // Cache the result in Redis
            await redisDatabase.StringSetAsync(accountHash.Data, JsonConvert.SerializeObject(accountData));

            return DHTResponse<AccountResponse>.Success("Account data fetched successfully.", accountData!);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<DHTResponse<AccountResponse>> StoreAccountDataAsync(StoreAccountDataRequest storeAccountDataRequest)
    {
        try
        {
            var accountHashResponse = await hashingService.ByteHash(storeAccountDataRequest.AccountNumber);

            // Store the data in the current node - ToDO: Implement this



            // Store data in Redis
            await redisDatabase.StringSetAsync(accountHashResponse.Data, JsonConvert.SerializeObject(storeAccountDataRequest));

            // Replicate data to neighboring nodes (implement using DHT logic)
            ReplicateAccountDataRequest replicateAccountDataRequest = new()
            {
                AccountHash = accountHashResponse.Data?? Array.Empty<byte>(),
                NodeUri = new Uri("") // ToDo: Get the right value

            };

            var replicationResult = await replicationService.ReplicateAccountDataAsync(replicateAccountDataRequest);

            if (!replicationResult.Data)
            {
                return DHTResponse<AccountResponse>.Failure(
                    "Error: Problem Storing Account Data",
                    new AccountResponse
                    {
                        AccountHash = storeAccountDataRequest.AccountHash,
                        AccountBIC = storeAccountDataRequest.BankBIC,
                        AccountNumber = storeAccountDataRequest.AccountNumber,
                        AccountName = storeAccountDataRequest.AccountName,
                    }!
                );
            }

            return DHTResponse<AccountResponse>.Success(
                "Account data stored successfully.",
                new AccountResponse 
                {
                    AccountHash = storeAccountDataRequest.AccountHash,
                    AccountBIC = storeAccountDataRequest.BankBIC,
                    AccountNumber = storeAccountDataRequest.AccountNumber,
                    AccountName = storeAccountDataRequest.AccountName,
                }!
            );

        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<DHTResponse<AccountResponse>> LookupAccountInMemoryAsync(AccountRequest accountRequest)
    {
        try
        {
            var accountHash = await hashingService.ByteHash(accountRequest.RecipientAccountNumber);
            var accountDataJson = await redisDatabase.StringGetAsync(accountHash.Data).ConfigureAwait(false);
            if (!accountDataJson.HasValue)
            {
                return DHTResponse<AccountResponse>.Failure("Account not found.");
            }

            var accountResponse = JsonConvert.DeserializeObject<AccountResponse>(accountDataJson!);
            return DHTResponse<AccountResponse>.Success("Account found", accountResponse!);
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<DHTResponse<NodeInfo>> FindResponsibleNodeAsync(byte[] accountHash)
    {
        try
        {
            // Start with the current node’s routing table
            var closestNodeResponse = await GetClosestNode(accountHash);
            if (closestNodeResponse.Data == null)
            {
                return DHTResponse<NodeInfo>.Failure("No closest node found.");
            }

            var closestNode = closestNodeResponse.Data;

            var nodeHasDataForKeyResult = await NodeHasDataForKeyAsync(closestNode, accountHash);
            if (nodeHasDataForKeyResult.Data)
            {
                return DHTResponse<NodeInfo>.Success("Node found and responsible for accountHash", closestNode);
            }

            // Loop through known nodes to find the closest responsible node
            while (true)
            {
                // Calculate the distance for the closest node
                long currentDistance = DHTUtilities.CalculateXorDistance(accountHash, closestNode.NodeHash);
                var nextClosestNodeResponse = await FindNextClosestNodeAsync(accountHash, currentDistance);

                // If the next closest node is the same as the current one, we've found the best match
                if (nextClosestNodeResponse.Data == null || nextClosestNodeResponse.Data.Equals(closestNode))
                {
                    return DHTResponse<NodeInfo>.Success("No closer node found", closestNode);
                }

                //closestNode = nextClosestNodeResponse.Data;
                closestNode = new NodeInfo
                {
                    NodeBIC = nextClosestNodeResponse.Data!.NodeBIC,
                    NodeHash = nextClosestNodeResponse.Data?.NodeHash ?? Array.Empty<byte>(),
                    NodeEndPoint = nextClosestNodeResponse.Data?.NodeEndPoint,
                    NodeUri = nextClosestNodeResponse.Data?.NodeUri ?? new Uri(string.Empty),
                    KnownPeers = nextClosestNodeResponse.Data?.KnownPeers,
                    LastSeen = DateTimeOffset.UtcNow,
                };

                // Check if this new closest node is responsible
                nodeHasDataForKeyResult = await NodeHasDataForKeyAsync(closestNode, accountHash);
                if (nodeHasDataForKeyResult.Data)
                {
                    return DHTResponse<NodeInfo>.Success("Node found and responsible for accountHash", closestNode);
                }

            }
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<DHTResponse<NodeInfo>> GetClosestNode(byte[] accountHash)
    {
        try
        {
            // Fetch all nodes from Redis (or only relevant ones based on some criteria)
            //var nodeKeys = await redisDatabase.HashKeysAsync("dht:nodes"); 
            var nodes = await dHTRedisService.GetAllNodesAsync(); 

            NodeInfo? closestNode = null;
            long closestDistance = long.MaxValue;

            foreach (var node in nodes.Data!)
            {
                //var serializedNode = await redisDatabase.HashGetAsync("dht:nodes", nodeKey);
                //if (serializedNode.IsNullOrEmpty)
                //{
                //    continue; 
                //}

                //var node = JsonConvert.DeserializeObject<NodeInfo>(serializedNode!);
                var distance = DHTUtilities.CalculateXorDistance(accountHash, node!.NodeHash);

                // Find the closest node based on the XOR distance
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestNode = node;
                }
            }

            if (closestNode != null)
            {
                return DHTResponse<NodeInfo>.Success("Success", closestNode);
            }

            return DHTResponse<NodeInfo>.Failure("No nodes found in the DHT.");
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<DHTResponse<NodeInfo>> FindNextClosestNodeAsync(byte[] accountHash, long currentDistance)
    {
        try
        {
            var nodeKeys = await redisDatabase.HashKeysAsync("dht:nodes");

            NodeInfo? nextClosestNode = null;
            long nextClosestDistance = long.MaxValue;

            // Step 2: Iterate over the nodes and calculate XOR distance
            foreach (var nodeKey in nodeKeys)
            {
                var serializedNode = await redisDatabase.HashGetAsync("dht:nodes", nodeKey);
                if (serializedNode.IsNullOrEmpty)
                {
                    continue;
                }

                var node = JsonConvert.DeserializeObject<NodeInfo>(serializedNode!);
                var distance = DHTUtilities.CalculateXorDistance(accountHash, node!.NodeHash);
                if (distance < nextClosestDistance && distance > currentDistance)
                {
                    nextClosestDistance = distance;
                    nextClosestNode = node;
                }
            }

            if (nextClosestNode != null)
            {
                return DHTResponse<NodeInfo>.Success("Next closest node found", nextClosestNode);
            }

            return DHTResponse<NodeInfo>.Failure("No next closest node found.");

        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<DHTResponse<bool>> NodeHasDataForKeyAsync(NodeInfo closestNode, byte[] accountHash)
    {
        try
        {
            // Check if the node has the data by checking Redis
            var nodeKey = $"node:{closestNode.NodeHash}";
            var hasData = await redisDatabase.HashExistsAsync(nodeKey, accountHash);
            return DHTResponse<bool>.Success("Check completed", hasData);
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task<List<NodeInfo>> GetKClosestNodesAsync(byte[] accountHash, int k = 20)
    {
        try
        {
            // Retrieve all nodes from Redis (local node's routing table)
            var allNodes = await redisDatabase.HashGetAllAsync("dht:nodes");

            if (allNodes.Length == 0)
            {
                return new List<NodeInfo>();
            }

            // Calculate XOR distance for each node and sort by closest
            var closestNodes = allNodes
                .Select(nodeEntry =>
                {
                    var nodeInfo = JsonConvert.DeserializeObject<NodeInfo>(nodeEntry.Value!);
                    var distance = DHTUtilities.CalculateXorDistance(accountHash, nodeInfo!.NodeHash);
                    return (Node: nodeInfo, Distance: distance);
                })
                .OrderBy(pair => pair.Distance)
                .Take(k)
                .Select(pair => pair.Node)
                .ToList();

            return closestNodes;
        }
        catch (Exception)
        {

            throw;
        }
        
    }

    public Task<DHTResponse<bool>> HasNextHop(NodeInfo currentNode, string targetHash)
    {
        throw new NotImplementedException();
    }

    public async Task<DHTResponse<AccountResponse>> QueryBankAsync(string bankBaseUrl, AccountRequest accountRequest)
    {
        try
        {
            // Create a Refit client for the specified bank
            var bankApiClient = apiClientFactory.CreateClient(bankBaseUrl);

            // Send request to the specified bank and return the result
            var accountDetails = await bankApiClient.FetchAccountData(accountRequest);

            return accountDetails;
        }
        catch (Exception)
        {

            throw;
        }
    }



}
