using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Confluent.Kafka;

using Newtonsoft.Json;

using Verify.Application.Abstractions.DHT;
using Verify.Application.Dtos.Account;
using Verify.Application.Dtos.Bank;
using Verify.Application.Dtos.Common;

namespace Verify.Infrastructure.Implementations.DHT;
internal sealed class ReplicationService : IReplicationService
{
    private readonly HttpClient httpClient;

    public ReplicationService(IHttpClientFactory httpClientFactory)
    {
        httpClient = httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(100);
    }

    public Task<DHTResponse<bool>> HandleNodeFailureAsync(NodeFailureRequest nodeFailureRequest)
    {
        throw new NotImplementedException();
    }

    public async Task<DHTResponse<bool>> ReplicateAccountDataAsync(ReplicateAccountDataRequest replicateAccountDataRequest)
    {
        try
        {
            var closestNeighbors = await GetNextClosestNodeAsync(replicateAccountDataRequest.AccountHash);
            if (closestNeighbors.Data != null)
            {
                Parallel.ForEach(closestNeighbors.Data, async neighbor =>
                {
                    await SendAccountDataToNeighbor(neighbor.NodeUri, replicateAccountDataRequest);
                });

                return DHTResponse<bool>.Success(
                    "",
                    true);
            }

            return DHTResponse<bool>.Success(
                    "",
                    false);

        }
        catch (Exception)
        {

            throw;
        }
    }

    #region Private Methods

    private async Task SendAccountDataToNeighbor(Uri neighborUri, ReplicateAccountDataRequest replicateAccountDataRequest)
    {
        // Serialize and send the account data to the neighbor
        var content = new StringContent(JsonConvert.SerializeObject(replicateAccountDataRequest), Encoding.UTF8, "application/json");
        await httpClient.PostAsync($"{neighborUri}/api/accounts/store", content);
    }

    #endregion


}
