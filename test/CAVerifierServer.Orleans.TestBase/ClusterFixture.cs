using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using CAVerifierServer;
using CAVerifierServer.Grains;
using CAVerifierServer.Grains.Grain;
using CAVerifierServer.Grains.Options;
using CAVerifierServer.Grains.State;
using EventStore.ClientAPI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Orleans;
using Orleans.Hosting;
using Orleans.TestingHost;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Reflection;

namespace AElfIndexer.Orleans.TestBase;

public class ClusterFixture:IDisposable,ISingletonDependency
{
    public ClusterFixture()
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
        //builder.AddClientBuilderConfigurator<TestClientBuilderConfigurator>();
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
                    services.AddMemoryCache();
                    services.AddDistributedMemoryCache();
                    services.AddAutoMapper(typeof(CAVerifierServerGrainsModule).Assembly);
                    
                    services.AddSingleton(typeof(IDistributedCache), typeof(MemoryDistributedCache));
                    services.AddSingleton(typeof(IDistributedCache<,>), typeof(DistributedCache<,>));
                   
                    services.Configure<AbpDistributedCacheOptions>(cacheOptions =>
                    {
                        cacheOptions.GlobalCacheEntryOptions.SlidingExpiration = TimeSpan.FromMinutes(20);
                    });
                    
                    services.OnExposing(onServiceExposingContext =>
                    {
                        //Register types for IObjectMapper<TSource, TDestination> if implements
                        onServiceExposingContext.ExposedTypes.AddRange(
                            ReflectionHelper.GetImplementedGenericTypes(
                                onServiceExposingContext.ImplementationType,
                                typeof(IObjectMapper<,>)
                            )
                        );
                    });
                    
                })
                
                
                // .AddRedisGrainStorageAsDefault(optionsBuilder => optionsBuilder.Configure(options =>
                // {
                //     options.DataConnectionString = "localhost:6379"; // This is the deafult
                //     options.UseJson = true;
                //     options.DatabaseNumber = 0;
                // }))
                .AddSimpleMessageStreamProvider(CAVerifierServerApplicationConsts.MessageStreamName)
                .AddMemoryGrainStorage("PubSubStore")
                .AddMemoryGrainStorageAsDefault();
        }
    }
    
}