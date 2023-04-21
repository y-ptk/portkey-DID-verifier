using AutoMapper;
using CAVerifierServer.Grain.Tests.GuardianIdentifier;
using CAVerifierServer.Grains;
using CAVerifierServer.Grains.Grain;
using CAVerifierServer.Grains.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Hosting;
using Orleans.TestingHost;
using Volo.Abp.AutoMapper;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;
using Volo.Abp.Timing;

namespace CAVerifierServer.Grain.Tests;

public class ClusterFixture : IDisposable, ISingletonDependency
{
    public ClusterFixture()
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
        builder.AddClientBuilderConfigurator<TestClientBuilderConfigurator>();
        Cluster = builder.Build();
        Cluster.Deploy();
    }

    public void Dispose()
    {
        Cluster.StopAllSilos();
    }

    public TestCluster Cluster { get; private set; }

    private class TestSiloConfigurations : ISiloBuilderConfigurator
    {
        public void Configure(ISiloHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices(services =>
                {
                    services.AddSingleton<IGuardianIdentifierVerificationGrain, GuardianIdentifierVerificationGrain>();
                    services.Configure<VerifierCodeOptions>(o =>
                    {
                        o.RetryTimes = 1;
                        o.CodeExpireTime = 2;
                        o.GetCodeFrequencyLimit = 1;
                        o.GetCodeFrequencyTimeLimit = 1;
                    });

                    

                    var dic = new Dictionary<string, int>
                    {
                        ["Email"] = 0,
                        ["Phone"] = 1
                    };
                    services.Configure<GuardianTypeOptions>(o =>
                    {
                        o.GuardianTypeDic = dic;
                    });

                    services.AddMemoryCache();
                    services.AddDistributedMemoryCache();
                    services.AddAutoMapper(typeof(CAVerifierServerGrainsModule).Assembly);

                    services.AddSingleton(typeof(DistributedCache<>));
                    services.AddSingleton(typeof(IDistributedCache<>), typeof(DistributedCache<>));
                    services.AddSingleton(typeof(IDistributedCache<,>), typeof(DistributedCache<,>));
                    services.Configure<AbpDistributedCacheOptions>(cacheOptions =>
                    {
                        cacheOptions.GlobalCacheEntryOptions.SlidingExpiration = TimeSpan.FromMinutes(20);
                    });
                    services.AddSingleton<ICancellationTokenProvider>(NullCancellationTokenProvider.Instance);
                    services.AddSingleton<IClock, MockClock>();
                })
                .AddSimpleMessageStreamProvider(CAVerifierServerApplicationConsts.MessageStreamName)
                .AddMemoryGrainStorage("PubSubStore")
                .AddMemoryGrainStorageAsDefault();
        }
    }

    public class MapperAccessor : IMapperAccessor
    {
        public IMapper Mapper { get; set; }
    }

    private class TestClientBuilderConfigurator : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder) => clientBuilder
            .AddSimpleMessageStreamProvider(CAVerifierServerApplicationConsts.MessageStreamName);
    }
}