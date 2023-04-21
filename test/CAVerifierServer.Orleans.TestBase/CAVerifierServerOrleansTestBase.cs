using AElfIndexer.Orleans.TestBase;
using Orleans.TestingHost;
using Volo.Abp.Modularity;

namespace CAVerifierServer.Orleans.TestBase;

public class CAVerifierServerOrleansTestBase<TStartupModule>:CAVerifierServerTestBase<TStartupModule> 
    where TStartupModule : IAbpModule
{
    
    protected readonly TestCluster Cluster;
    
    public CAVerifierServerOrleansTestBase()
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
    }
}