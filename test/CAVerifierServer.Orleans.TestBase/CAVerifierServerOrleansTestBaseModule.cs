using AElfIndexer.Orleans.TestBase;
using CAVerifierServer.Grains;
using CAVerifierServer.Grains.Options;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp.Modularity;

namespace CAVerifierServer.Orleans.TestBase;

public class CAVerifierServerOrleansTestBaseModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IClusterClient>(sp => sp.GetService<ClusterFixture>().Cluster.Client);
        context.Services.Configure<VerifierAccountOptions>(o =>
        {
            o.PrivateKey = "XXXXXXX";
            o.Address = "XXXXXX";
        });
        context.Services.Configure<VerifierCodeOptions>(o =>
        {
            o.RetryTimes = 2;
            o.CodeExpireTime = 5;
            o.GetCodeFrequencyLimit = 1;
            o.GetCodeFrequencyTimeLimit = 1;
        });
    }
}