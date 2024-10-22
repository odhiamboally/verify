using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verify.Application.Abstractions.DHT;
using Verify.Application.Abstractions.Interfaces;
using Verify.Application.Abstractions.IServices;

namespace Verify.Infrastructure.Implementations.Interfaces;
internal sealed class ServiceManager : IServiceManager
{
    public IAccountService AccountService { get; }
    public ICacheService CacheService { get; }
    public ILogService LogService { get; }
    public IDHTService DHTService { get; }
    public IDHTRedisService DHTRedisService { get; }
    public IHashingService HashingService { get; }
    public INodeHealthCheckService NodeHealthCheckService { get; }




    public ServiceManager(
        IAccountService accountService, 
        ICacheService cacheService, 
        ILogService logService, 
        IDHTService dHTService,
        IDHTRedisService dHTRedisService,
        IHashingService hashingService, 
        INodeHealthCheckService nodeHealthCheckService)
    {
        AccountService = accountService;
        CacheService = cacheService;
        LogService = logService;
        DHTService = dHTService;
        DHTRedisService = dHTRedisService;
        HashingService = hashingService;
        NodeHealthCheckService = nodeHealthCheckService;
    }
}
