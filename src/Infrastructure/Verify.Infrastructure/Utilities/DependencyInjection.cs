using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

using Quartz;
using Confluent.Kafka;
using MassTransit;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

using Verify.Application.Abstractions.DHT;
using Verify.Application.Abstractions.Interfaces;
using Verify.Application.Abstractions.IRepositories;
using Verify.Application.Abstractions.IServices;
using Verify.Application.Abstractions.MessageQueuing;
using Verify.Infrastructure.Configurations.Caching;
using Verify.Infrastructure.Configurations.Common;
using Verify.Infrastructure.Implementations.Caching;
using Verify.Infrastructure.Implementations.DHT;
using Verify.Infrastructure.Implementations.Interfaces;
using Verify.Infrastructure.Implementations.MessageQueuing.MessageConsumers;
using Verify.Infrastructure.Implementations.MessageQueuing.Transports.Kafka;
using Verify.Infrastructure.Implementations.MessageQueuing.Transports.RabbitMQ;
using Verify.Infrastructure.Implementations.Repositories;
using Verify.Infrastructure.Implementations.Services;
using Verify.Persistence.DataContext;
using Refit;
using Verify.Infrastructure.Utilities.DHT.ApiClients;

namespace Verify.Infrastructure.Utilities;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        try
        {
            var cacheSettings = new CacheSetting();
            configuration.GetSection("CacheSettings").Bind(cacheSettings);
            services.AddSingleton(cacheSettings);
            CacheKeyGenerator.Configure(cacheSettings);

            var paginationSetting = new PaginationSetting();
            configuration.GetSection("PaginationSetting").Bind(paginationSetting);
            services.AddSingleton(paginationSetting);

            var ConnString = configuration.GetConnectionString("KHS");
            services.AddDbContext<DBContext>(options => options.UseSqlServer(ConnString!));

            services.AddScoped<IServiceManager, ServiceManager>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<ILogService, LogService>();
            services.AddScoped<IDHTService, DHTService>();
            services.AddScoped<IHashingService, HashingService>();
            services.AddScoped<IReplicationService, ReplicationService>();
            services.AddScoped<INodeHealthCheckService, NodeHealthCheckService>();

            var refitSettings = new RefitSettings(); // Customize if needed
            services.AddSingleton<IApiClientFactory>(new ApiClientFactory(refitSettings));

            // Kafka ProducerConfig
            services.AddScoped<IMessageProducer, KafkaMessageProducer>();
            services.AddSingleton(new ProducerConfig { BootstrapServers = "localhost:9092" });

            // Kafka consumer configuration
            services.AddSingleton<IMessageConsumer, KafkaMessageConsumer>();
            services.AddSingleton(new ConsumerConfig
            {
                BootstrapServers = "localhost:9092",
                GroupId = "my-consumer-group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            });

            // MassTransit and RabbitMQ configuration
            services.AddScoped<IMessageProducer, RabbitMqBusMessageProducer>();
            services.AddScoped<IMessageProducer, RabbitMqBusControlMessageProducer>();
            // RabbitMQ consumer
            services.AddSingleton<IMessageConsumer, RabbitMqBusControlMessageConsumer>();
            services.AddMassTransit(x =>
            {
                x.AddConsumer<AccountMessageConsumer>();

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host("localhost", "/", h =>
                    {
                        h.Username("guest");
                        h.Password("guest");

                        h.UseSsl(s =>
                        {
                            s.Protocol = SslProtocols.Tls12;
                        });
                    });

                    // Connect the consumer to a receive endpoint
                    cfg.ReceiveEndpoint("my-queue", e =>
                    {
                        e.ConfigureConsumer<AccountMessageConsumer>(context);
                    });

                });
            });

            // Register Quartz services
            services.AddQuartz(q =>
            {
                // Schedule the NodeHealthCheckJob to run every 5 minutes
                //q.UseMicrosoftDependencyInjectionJobFactory();

                var jobKey = new JobKey("NodeHealthCheckService");
                q.AddJob<NodeHealthCheckService>(opts => opts.WithIdentity(jobKey));

                q.AddTrigger(opts => opts
                    .ForJob(jobKey)
                    .WithIdentity("NodeHealthCheckJob-trigger")
                    .WithSimpleSchedule(x => x
                        .WithInterval(TimeSpan.FromMinutes(5))
                        .RepeatForever())
                );
            });

            // Add the Quartz hosted service
            services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

            switch (cacheSettings.CacheType)
            {
                case string type when type.Equals("redis", StringComparison.OrdinalIgnoreCase):

                    services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(cacheSettings.Redis!.Configuration!));

                    services.AddStackExchangeRedisCache(options =>
                    {
                        options.Configuration = cacheSettings.Redis!.Configuration;
                        options.InstanceName = cacheSettings.Redis.InstanceName;
                    });
                    services.AddSingleton<ICacheService, RedisMultiplexerCacheService>();
                    services.AddSingleton<ICacheService, RedisCacheService>();
                    break;

                case string type when type.Equals("azure", StringComparison.OrdinalIgnoreCase):
                    services.AddStackExchangeRedisCache(options =>
                    {
                        options.Configuration = cacheSettings.Azure!.ConnectionString;
                    });
                    services.AddSingleton<ICacheService, AzureCacheService>();
                    break;

                case string type when type.Equals("aws", StringComparison.OrdinalIgnoreCase):
                    services.AddStackExchangeRedisCache(options =>
                    {
                        options.Configuration = cacheSettings.Aws!.Endpoint;
                    });
                    services.AddSingleton<ICacheService, ElastiCacheService>();
                    break;

                default:
                    services.AddMemoryCache();
                    services.AddSingleton<ICacheService, InMemoryCacheService>();
                    break;
            }

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddTransient(typeof(IBaseRepository<>), typeof(BaseRepository<>));

            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<ILogRepository, LogRepository>();

            return services;
        }
        catch (Exception)
        {

            throw;
        }



    }
}

