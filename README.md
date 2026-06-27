# Verify - Production DHT Implementation

## 🏢 Overview

**Verify** is a production-grade Distributed Hash Table (DHT) system for account verification in banking networks. It implements the Kademlia routing algorithm for efficient peer-to-peer account lookups across distributed nodes.

**Status**: Production (Stable)  
**Purpose**: Account verification, node routing, DHT operations  
**Branch Strategy**: Main production branch - stable, tested, battle-hardened

---

## ✨ Features

- **Kademlia DHT Algorithm** - XOR-based distance metric and k-bucket routing
- **Account Lookup** - Fast distributed account verification across banking network
- **Node Management** - Dynamic node addition, removal, and bucket management
- **Multi-Backend Caching** - Redis, Azure Cache, AWS ElastiCache, or In-Memory
- **REST API** - Simple HTTP endpoints for account operations
- **Error Handling** - Production-grade exception handling with detailed error responses
- **Load Testing** - K6 scripts for performance validation

---

## 🏗️ Architecture

```
verify/
├── src/
│   ├── Domain/                          # Entities, enums, value objects
│   ├── Application/                     # Business logic, DTOs, abstractions
│   ├── Infrastructure/
│   │   ├── DHT/
│   │   │   ├── DhtService.cs           # Main DHT orchestration
│   │   │   ├── DhtRedisService.cs      # Redis operations
│   │   │   ├── NodeManagementService.cs # Node lifecycle management
│   │   ├── Caching/                     # Cache abstraction layer
│   │   │   ├── RedisMultiplexerCacheService.cs
│   │   │   ├── RedisCacheService.cs
│   │   │   ├── AzureCacheService.cs
│   │   │   ├── ElastiCacheService.cs
│   │   │   └── InMemoryCacheService.cs
│   │   └── Utilities/
│   │       ├── DependencyInjection.cs  # Service registration
│   │       └── DhtUtilities.cs         # XOR distance calculations
│   ├── Api/
│   │   ├── Program.cs                  # Configuration & DI
│   │   ├── Middleware/ApiExceptionHandler.cs
│   │   ├── Controllers/DhtController.cs
│   │   └── loadtest.js                 # K6 performance tests
│   └── Web/                             # Client libraries
│       └── Web.Client.Console/         # Console client
```

### Core Concepts

1. **Kademlia Algorithm**
   - XOR distance metric for node proximity
   - K-buckets for node organization
   - Iterative lookup for finding responsible nodes

2. **DHT Operations**
   - `FetchAccountData()` - Find account across network
   - `StoreAccountDataAsync()` - Store account locally
   - `LookupAccountInMemoryAsync()` - Cache lookup
   - `FindClosestResponsibleNodeAsync()` - Route to responsible node

3. **Node Management**
   - Dynamic node discovery
   - Bucket management with eviction policies
   - Least-Recently-Seen (LRS) replacement

---

## 📚 API Endpoints

### Account Operations

**POST /api/dht/fetchaccountinfo**
```json
// Request
{
  "initiatorBIC": "BANKABCDEF",
  "recipientBIC": "BANKXYZABC",
  "recipientAccountNumber": "123456789"
}

// Response
{
  "accountHash": "...",
  "accountName": "John Doe",
  "accountNumber": "123456789",
  "accountBic": "BANKXYZABC"
}
```

---

## 🔧 Configuration

### appsettings.json
```json
{
  "CacheSettings": {
    "CacheType": "redis",
    "Redis": {
      "Configuration": "localhost:6379",
      "InstanceName": "verify_"
    }
  },
  "NodeConfig": {
    "CurrentNode": "YOUR_BANK_BIC",
    "DHTNODE": "https://localhost:7260"
  }
}
```

### Cache Backends
- **redis** - StackExchange.Redis
- **azure** - Azure Cache for Redis
- **aws** - AWS ElastiCache
- **default** - In-Memory cache

---

## 🚀 Getting Started

### Prerequisites
- .NET 8.0+
- Redis (or other cache backend)
- Docker (optional)

### Installation

```bash
# Clone repository
git clone https://github.com/odhiamboally/verify.git
cd verify

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run --project src/Api/Verify.Api
```

### Docker

```bash
# Build image
docker build -t verify:latest .

# Run with Redis
docker run -d \
  --name verify \
  -p 7260:7260 \
  -e CacheSettings__CacheType=redis \
  -e CacheSettings__Redis__Configuration=redis:6379 \
  verify:latest
```

---

## 📊 Performance Characteristics

| Operation | Time | Notes |
|-----------|------|-------|
| Account Lookup | ~50-200ms | Depends on network depth |
| Node Addition | ~10ms | O(log n) bucket operations |
| Cache Hit | <1ms | In-memory or Redis |
| XOR Distance Calc | <0.1ms | Bitwise operations |

---

## 🔄 Deployment

### Prerequisites
1. Redis cluster (or cache backend)
2. Database (for logging)
3. Network connectivity to DHT peers

### Recommended Setup

```
Load Balancer
    ↓
[Verify Instance 1]
[Verify Instance 2]  → Redis Cluster
[Verify Instance 3]
    ↓
[Banking Network Peers]
```

---

## 🧪 Testing

### Load Testing with K6

```bash
# Install K6
# macOS: brew install k6
# Linux: sudo apt-get install k6
# Windows: choco install k6

# Run tests
k6 run src/Api/Verify.Api/loadtest.js

# Custom configuration
k6 run -e TARGET_URL=https://your-api.com loadtest.js
```

### Unit Tests

```bash
dotnet test
```

---

## 🔐 Security Considerations

- **Input Validation** - FluentValidation on all requests
- **Exception Handling** - Sanitized error messages (no stack traces to client)
- **HTTPS** - Enforce in production
- **Redis Authentication** - Configure credentials in appsettings
- **Rate Limiting** - Consider implementing per-node rate limits
- **Node Verification** - Validate node signatures before adding to DHT

---

## 📈 Monitoring & Observability

### Key Metrics to Track
- Account lookup latency (p50, p95, p99)
- Cache hit ratio
- Node availability
- Request error rate
- Redis connection pool status

### Logging
Built-in structured logging to console and database

```csharp
_logger.LogInformation("Account lookup for {AccountNumber}", accountNumber);
_logger.LogError(ex, "Failed to retrieve account from node");
```

---

## 🆚 Verify vs Verify V2

### When to Use This (verify)

✅ **Production deployments**  
✅ **Stable, proven implementation**  
✅ **Simpler codebase for maintenance**  
✅ **Standard JSON serialization**  
✅ **Manual node management**  

### When to Consider Verify V2

⚠️ **High-volume, high-throughput scenarios**  
⚠️ **Performance is critical**  
⚠️ **Auto-maintenance desired**  
⚠️ **MessagePack serialization benefits**  

### Migration Strategy

See [MIGRATION.md](./MIGRATION.md) for details on gradually migrating to verify_v2 using feature flags.

---

## 🛠️ Technology Stack

- **Runtime**: .NET 8.0+
- **Web Framework**: ASP.NET Core
- **Caching**: StackExchange.Redis, Azure Cache
- **Validation**: FluentValidation
- **Serialization**: System.Text.Json (JSON), Newtonsoft.Json
- **Logging**: Microsoft.Extensions.Logging
- **Testing**: xUnit, K6
- **ORM**: Entity Framework Core

---

## 📋 Common Tasks

### Add a New Node to DHT

```csharp
var newNode = new NodeInfo
{
    NodeBIC = "NEWBANKBIC",
    NodeHash = await hashingService.ByteHash("NEWBANKBIC"),
    NodeUri = new Uri("https://newbank.com:7260"),
    NodeEndPoint = "https://newbank.com:7260"
};

var result = await nodeManagementService.AddOrUpdateNodeAsync(newNode);
```

### Query Account from Network

```csharp
var request = new AccountRequest
{
    InitiatorBIC = "BANKABCDEF",
    RecipientBIC = "BANKXYZABC",
    RecipientAccountNumber = "123456789"
};

var result = await dhtService.FetchAccountData(request);
```

### Change Cache Backend

Update `appsettings.json`:
```json
"CacheSettings": {
  "CacheType": "azure",  // Change to azure, aws, or memory
  "Azure": {
    "ConnectionString": "your-connection-string"
  }
}
```

---

## 🐛 Troubleshooting

### Issue: "Account not found"
- **Check**: Is the responsible node online?
- **Check**: Has the account been stored on that node?
- **Solution**: Add node to DHT and trigger account replication

### Issue: "Connection timeout"
- **Check**: Redis connectivity
- **Check**: Network connectivity to peer nodes
- **Solution**: Verify firewall rules, check Redis health

### Issue: "Bucket is full"
- **Cause**: All k-bucket slots filled, node not reachable
- **Solution**: Nodes will be evicted based on LRS policy when unreachable nodes are detected

---

## 📚 Resources

- [Kademlia Paper](https://pdos.csail.mit.edu/~petar/papers/maymounkov-kademlia-lncs.pdf)
- [DHT Wikipedia](https://en.wikipedia.org/wiki/Distributed_hash_table)
- [XOR Metric Routing](https://en.wikipedia.org/wiki/Kademlia#Routing_tables)

---

## 🤝 Contributing

1. Create feature branch: `git checkout -b feature/your-feature`
2. Commit changes: `git commit -am 'Add feature'`
3. Push to branch: `git push origin feature/your-feature`
4. Create Pull Request

### Code Guidelines
- Follow C# naming conventions (PascalCase for public, camelCase for private)
- Add XML documentation for public methods
- Write unit tests for new features
- Keep methods focused and small

---

## 📝 License

[Your License Here]

---

## 👥 Support

For issues or questions:
- Open an issue on GitHub
- Check existing documentation
- Review test cases for usage examples

---

## 🔮 Future Roadmap

- [ ] Performance optimizations from verify_v2
- [ ] gRPC service layer
- [ ] Distributed tracing with OpenTelemetry
- [ ] Kubernetes Helm charts
- [ ] GraphQL API layer
- [ ] Web dashboard UI
