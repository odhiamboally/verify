using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Confluent.Kafka;

using MassTransit.Internals.GraphValidation;

using Microsoft.Extensions.Logging;

using Quartz;
using Quartz.Logging;

using Verify.Application.Abstractions.DHT;

namespace Verify.Infrastructure.Implementations.DHT;
internal sealed class NodeHealthCheckService : INodeHealthCheckService
{
    private readonly INodeManagementService nodeManagementService;

    public NodeHealthCheckService(INodeManagementService NodeManagementService)
    {
        nodeManagementService = NodeManagementService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var bucketsResponse = await nodeManagementService.GetAllBucketsAsync();

            if (bucketsResponse.Data != null && bucketsResponse.Data.Count > 0)
            {
                foreach (var bucket in bucketsResponse.Data)
                {
                    // Implement a Bucket object that holds a list of 'AddNodeToRoutingTableRequest' - (since buckets hold references to other nodes in the network)

                    var isReachableResponse = await nodeManagementService.PingNodeAsync(bucket);
                    bool isReachable = isReachableResponse.Data;
                    if (!isReachable)
                    {
                        // Remove the node if unreachable
                        await nodeManagementService.RemoveNodeFromBucketAsync(bucket);
                    }
                }

            }

        }
        catch (Exception)
        {
            throw;
        }
    }




    #region Private Methods

    

    #endregion



}
