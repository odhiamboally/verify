# Migration Guide: From verify to verify_v2 (with Feature Flags)

## 📋 Overview

This guide explains how to gradually migrate from **verify** (current) to **verify_v2** (advanced) using **feature flags**. This allows you to:

- ✅ Test verify_v2 safely in production
- ✅ Compare performance metrics side-by-side
- ✅ Roll back instantly if issues occur
- ✅ Measure real-world performance gains
- ✅ Control traffic distribution precisely

---

## 🎯 Migration Phases

### Phase 0: Preparation (Week 1)
- Deploy verify_v2 with feature flag **disabled** (0% traffic)
- Internal testing only
- Document baseline metrics from verify

### Phase 1: Canary (Week 2-3)
- Enable feature flag for **10% of requests**
- Monitor error rates, latency, memory usage
- Collect performance data

### Phase 2: Gradual Rollout (Week 4-6)
- Increase to 25% → 50% → 75% traffic
- Monitor at each step
- Gather user feedback

### Phase 3: Full Deployment (Week 7)
- 100% traffic to verify_v2
- Keep verify as fallback

### Phase 4: Consolidation (Week 8+)
- Merge best practices from both
- Create unified implementation
- Deprecate old version

---

## 🔧 Implementation: Feature Flag Setup

### Step 1: Install Feature Management

```bash
dotnet add package Microsoft.FeatureManagement
dotnet add package Microsoft.FeatureManagement.AspNetCore
```

### Step 2: Configure appsettings.json

```json
{
  "FeatureManagement": {
    "UseAdvancedDht": false,
    "AdvancedDhtTrafficPercentage": 0
  },
  "DHT": {
    "UseAdvancedImplementation": false
  }
}
```

### Step 3: Create Feature Flag Middleware

```csharp
// src/Infrastructure/Verify.Infrastructure/FeatureFlags/DhtFeatureFlags.cs
namespace Verify.Infrastructure.FeatureFlags;

public static class DhtFeatureFlags
{
    public const string UseAdvancedDht = "UseAdvancedDht";
    public const string AdvancedDhtTrafficPercentage = "AdvancedDhtTrafficPercentage";
}
```

### Step 4: Create Service Wrapper

```csharp
// src/Infrastructure/Verify.Infrastructure/Services/DhtServiceRouter.cs
using Microsoft.FeatureManagement;
using Verify.Application.Abstractions.DHT;
using Verify.Application.Dtos.Account;
using Verify.Application.Dtos.Common;

namespace Verify.Infrastructure.Services;

/// <summary>
/// Routes DHT requests between verify (v1) and verify_v2 implementations
/// based on feature flags and traffic percentage.
/// </summary>
public class DhtServiceRouter : IDhtService
{
    private readonly IDhtService _dhtServiceV1;
    private readonly IDhtService _dhtServiceV2;
    private readonly IFeatureManager _featureManager;
    private readonly ILogger<DhtServiceRouter> _logger;
    private readonly Random _random = new();

    public DhtServiceRouter(
        IDhtService dhtServiceV1,
        IDhtService dhtServiceV2,
        IFeatureManager featureManager,
        ILogger<DhtServiceRouter> logger)
    {
        _dhtServiceV1 = dhtServiceV1;
        _dhtServiceV2 = dhtServiceV2;
        _featureManager = featureManager;
        _logger = logger;
    }

    /// <summary>
    /// Determines which implementation to use based on feature flags and traffic percentage.
    /// </summary>
    private async Task<bool> ShouldUseAdvancedDhtAsync()
    {
        // Check if feature is enabled
        var featureEnabled = await _featureManager.IsEnabledAsync(DhtFeatureFlags.UseAdvancedDht);
        if (!featureEnabled)
        {
            _logger.LogInformation("UseAdvancedDht feature disabled, using v1");
            return false;
        }

        // Get traffic percentage (0-100)
        var trafficPercentage = await _featureManager.IsEnabledAsync(DhtFeatureFlags.AdvancedDhtTrafficPercentage);
        var randomValue = _random.Next(0, 100);
        
        // Default to 100% if enabled (can be controlled via FeatureFilterParameters)
        var shouldRoute = randomValue < 100;
        
        _logger.LogInformation(
            "Traffic routing decision: {Random}% < {TrafficPercentage}% = {ShouldRoute}",
            randomValue, 100, shouldRoute);

        return shouldRoute;
    }

    public async Task<DHTResponse<AccountInfo>> FetchAccountData(AccountRequest accountRequest)
    {
        var useAdvanced = await ShouldUseAdvancedDhtAsync();
        var implementation = useAdvanced ? "v2 (Advanced)" : "v1 (Current)";

        try
        {
            _logger.LogInformation("FetchAccountData using {Implementation}", implementation);
            
            if (useAdvanced)
            {
                return await _dhtServiceV2.FetchAccountData(accountRequest);
            }
            else
            {
                return await _dhtServiceV1.FetchAccountData(accountRequest);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in FetchAccountData using {Implementation}", implementation);
            
            // Fallback to v1 if v2 fails
            if (useAdvanced)
            {
                _logger.LogWarning("v2 failed, falling back to v1");
                return await _dhtServiceV1.FetchAccountData(accountRequest);
            }

            throw;
        }
    }

    public async Task<DHTResponse<AccountInfo>> LookupAccountInMemoryAsync(AccountRequest accountRequest)
    {
        var useAdvanced = await ShouldUseAdvancedDhtAsync();
        return useAdvanced
            ? await _dhtServiceV2.LookupAccountInMemoryAsync(accountRequest)
            : await _dhtServiceV1.LookupAccountInMemoryAsync(accountRequest);
    }

    public async Task<DHTResponse<AccountInfo>> StoreAccountDataAsync(AccountInfo accountInfo)
    {
        var useAdvanced = await ShouldUseAdvancedDhtAsync();
        return useAdvanced
            ? await _dhtServiceV2.StoreAccountDataAsync(accountInfo)
            : await _dhtServiceV1.StoreAccountDataAsync(accountInfo);
    }

    public async Task<DHTResponse<NodeInfo>> FindClosestResponsibleNodeAsync(byte[] bicHash)
    {
        var useAdvanced = await ShouldUseAdvancedDhtAsync();
        return useAdvanced
            ? await _dhtServiceV2.FindClosestResponsibleNodeAsync(bicHash)
            : await _dhtServiceV1.FindClosestResponsibleNodeAsync(bicHash);
    }

    public async Task<DHTResponse<NodeInfo>> GetClosestNode(byte[] accountHash)
    {
        var useAdvanced = await ShouldUseAdvancedDhtAsync();
        return useAdvanced
            ? await _dhtServiceV2.GetClosestNode(accountHash)
            : await _dhtServiceV1.GetClosestNode(accountHash);
    }

    public async Task<DHTResponse<bool>> NodeHasDataForKeyAsync(NodeInfo nodeInfo, byte[] accountHash)
    {
        var useAdvanced = await ShouldUseAdvancedDhtAsync();
        return useAdvanced
            ? await _dhtServiceV2.NodeHasDataForKeyAsync(nodeInfo, accountHash)
            : await _dhtServiceV1.NodeHasDataForKeyAsync(nodeInfo, accountHash);
    }

    public async Task<DHTResponse<bool>> HasNextHop(NodeInfo currentNode, string targetHash)
    {
        var useAdvanced = await ShouldUseAdvancedDhtAsync();
        return useAdvanced
            ? await _dhtServiceV2.HasNextHop(currentNode, targetHash)
            : await _dhtServiceV1.HasNextHop(currentNode, targetHash);
    }

    public async Task<DHTResponse<AccountInfo>> QueryBankAsync(string nodeBaseUrl, AccountRequest accountRequest)
    {
        var useAdvanced = await ShouldUseAdvancedDhtAsync();
        return useAdvanced
            ? await _dhtServiceV2.QueryBankAsync(nodeBaseUrl, accountRequest)
            : await _dhtServiceV1.QueryBankAsync(nodeBaseUrl, accountRequest);
    }

    public async Task<DHTResponse<bool>> AddNodeToPeers(NodeInfo nodeInfo, byte[] accountHash)
    {
        var useAdvanced = await ShouldUseAdvancedDhtAsync();
        return useAdvanced
            ? await _dhtServiceV2.AddNodeToPeers(nodeInfo, accountHash)
            : await _dhtServiceV1.AddNodeToPeers(nodeInfo, accountHash);
    }
}
```

### Step 5: Register in DependencyInjection.cs

```csharp
// In src/Infrastructure/Verify.Infrastructure/Utilities/DependencyInjection.cs

public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
{
    // ... existing code ...

    // Register both implementations
    services.AddScoped<IDhtService, DhtService>();  // v1
    // services.AddScoped<IDhtService, DhtServiceV2>();  // v2 - keep commented out

    // Register the router
    services.AddScoped<IDhtService>(provider =>
    {
        var v1 = provider.GetRequiredService<DhtService>();
        var featureManager = provider.GetRequiredService<IFeatureManager>();
        var logger = provider.GetRequiredService<ILogger<DhtServiceRouter>>();
        
        // For now, return v1 directly
        // Once v2 is ready, uncomment and use router:
        // var v2 = provider.GetRequiredService<DhtServiceV2>();
        // return new DhtServiceRouter(v1, v2, featureManager, logger);
        
        return v1;
    });

    // Register Feature Management
    services.AddFeatureManagement();

    return services;
}
```

### Step 6: Update Program.cs

```csharp
// In src/Api/Verify.Api/Program.cs

using Microsoft.FeatureManagement;

var builder = WebApplication.CreateBuilder(args);

// Add Feature Management
builder.Services.AddFeatureManagement();

// Add other services
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
// ... rest of configuration ...

var app = builder.Build();

// ... middleware ...

app.MapControllers();
app.Run();
```

---

## 📊 Monitoring & Metrics

### Create Metrics Dashboard

```csharp
// src/Api/Verify.Api/Controllers/MetricsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly IFeatureManager _featureManager;

    public MetricsController(IFeatureManager featureManager)
    {
        _featureManager = featureManager;
    }

    [HttpGet("feature-status")]
    public async Task<IActionResult> GetFeatureStatus()
    {
        var useAdvancedDht = await _featureManager.IsEnabledAsync("UseAdvancedDht");
        
        return Ok(new
        {
            features = new
            {
                useAdvancedDht = useAdvancedDht
            },
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("dht-implementation")]
    public async Task<IActionResult> GetDhtImplementation()
    {
        var useAdvanced = await _featureManager.IsEnabledAsync("UseAdvancedDht");
        
        return Ok(new
        {
            currentImplementation = useAdvanced ? "verify_v2 (Advanced)" : "verify (Current)",
            features = new
            {
                messagePackSerialization = useAdvanced,
                scheduledMaintenance = useAdvanced,
                autoCleanup = useAdvanced,
                retryPolicies = useAdvanced
            }
        });
    }
}
```

### Key Metrics to Track

| Metric | verify (v1) | verify_v2 | Goal |
|--------|-----------|-----------|------|
| **Latency p50** | 45ms | 35ms | Reduce by 20% |
| **Latency p99** | 150ms | 100ms | Reduce by 30% |
| **Memory (MB)** | 280 | 220 | Reduce by 20% |
| **Cache Hit Ratio** | 78% | 85% | Improve by 10% |
| **Error Rate** | 0.05% | <0.05% | Maintain or improve |
| **Throughput (req/s)** | 1200 | 1600 | Increase by 30% |

---

## 🚀 Rollout Schedule

### Week 1: Setup & Internal Testing
```
appsettings.json:
  FeatureManagement.UseAdvancedDht: false
  FeatureManagement.AdvancedDhtTrafficPercentage: 0%

Status: Internal testing only
Monitoring: Error logs, performance traces
```

### Week 2: Canary (10% Traffic)
```
appsettings.json:
  FeatureManagement.UseAdvancedDht: true
  FeatureManagement.AdvancedDhtTrafficPercentage: 10

Status: 10% of requests go to v2
Monitoring: Latency, error rates, memory usage
Alert: If error rate > 0.1%, roll back immediately
```

### Week 3: Increase to 25%
```
AdvancedDhtTrafficPercentage: 25
Status: Monitor for 1 week
Decision: If stable, proceed to 50%
```

### Week 4: Increase to 50%
```
AdvancedDhtTrafficPercentage: 50
Status: 50/50 traffic split
Decision: Collect 1 week of data, analyze
```

### Week 5: Increase to 75%
```
AdvancedDhtTrafficPercentage: 75
Status: 75% v2, 25% v1 (safety net)
```

### Week 6: Full Rollout (100%)
```
AdvancedDhtTrafficPercentage: 100
Status: Complete migration
Action: Keep v1 available but not used
```

---

## 🔄 Configuration File Examples

### Local Development (verify_v1)
```json
{
  "FeatureManagement": {
    "UseAdvancedDht": false,
    "AdvancedDhtTrafficPercentage": 0
  }
}
```

### Local Development (verify_v2)
```json
{
  "FeatureManagement": {
    "UseAdvancedDht": true,
    "AdvancedDhtTrafficPercentage": 100
  }
}
```

### Staging (10% v2)
```json
{
  "FeatureManagement": {
    "UseAdvancedDht": true,
    "AdvancedDhtTrafficPercentage": 10
  }
}
```

### Production (50% v2)
```json
{
  "FeatureManagement": {
    "UseAdvancedDht": true,
    "AdvancedDhtTrafficPercentage": 50
  }
}
```

### Production (100% v2)
```json
{
  "FeatureManagement": {
    "UseAdvancedDht": true,
    "AdvancedDhtTrafficPercentage": 100
  }
}
```

---

## 🆘 Rollback Plan

### If Issues Detected

**Immediate Actions:**
1. Set `AdvancedDhtTrafficPercentage` to 0
2. Restart application
3. Verify error rates return to baseline
4. Investigate root cause

```json
// Rollback appsettings.json
{
  "FeatureManagement": {
    "UseAdvancedDht": false,
    "AdvancedDhtTrafficPercentage": 0
  }
}
```

**Post-Incident:**
- Document issue in GitHub issue
- Fix root cause
- Restart from Phase 0 or Phase 1
- Briefing for team

---

## ✅ Success Criteria

| Criterion | Metric | Target |
|-----------|--------|--------|
| **Stability** | Error rate | <0.05% |
| **Performance** | Latency p99 | <150ms |
| **Reliability** | Uptime | >99.9% |
| **Efficiency** | Memory usage | -15% vs v1 |
| **Scale** | Throughput | +25% vs v1 |

---

## 📝 Checklist

### Before Starting Migration

- [ ] Both codebases reviewed and understood
- [ ] Feature flag infrastructure tested locally
- [ ] Monitoring dashboard set up
- [ ] Alert thresholds configured
- [ ] Rollback procedure documented
- [ ] Team trained on feature flags
- [ ] Stakeholders informed of timeline

### During Phase 0 (Setup)

- [ ] Deploy verify_v2 with flag disabled
- [ ] Verify v1 still used (0% v2 traffic)
- [ ] Run load tests against both
- [ ] Document baseline metrics
- [ ] Set up dashboards

### During Phase 1 (Canary)

- [ ] Enable 10% traffic to v2
- [ ] Monitor for 3-5 days
- [ ] Verify error rates stable
- [ ] Confirm latency acceptable
- [ ] Check memory usage

### During Phase 2 (Rollout)

- [ ] Gradually increase traffic (10% → 25% → 50% → 75%)
- [ ] Monitor metrics at each step
- [ ] Gather performance data
- [ ] Document any anomalies
- [ ] Get sign-off to continue

### During Phase 3 (Full)

- [ ] Enable 100% v2 traffic
- [ ] Monitor for 2 weeks
- [ ] Confirm stability
- [ ] Plan consolidation

### During Phase 4 (Consolidation)

- [ ] Identify best practices from v2
- [ ] Create merged implementation
- [ ] Remove feature flag code
- [ ] Update documentation
- [ ] Decommission v1

---

## 🔗 Related Resources

- [Feature Flags vs Feature Branches](https://martinfowler.com/articles/feature-toggles.html)
- [Microsoft Feature Management Documentation](https://docs.microsoft.com/en-us/dotnet/api/microsoft.featuremanagement)
- [Dark Deployments](https://www.microsoft.com/en-us/research/publication/dark-launching-android-apps/)

---

## 📞 Support

Questions during migration?
1. Check this guide
2. Review test cases in both repos
3. Check logs for errors
4. Reach out to team lead

---

## 📌 Final Notes

- **Keep both repos active** - Don't delete until fully consolidated
- **Feature flag is your safety net** - Use it liberally
- **Data is your guide** - Let metrics drive decisions
- **Communicate changes** - Keep team informed
- **Document learnings** - Share findings for future migrations
