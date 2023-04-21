using CAVerifierServer.Grains.Options;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace CAVerifierServer.Grains;
[DependsOn(typeof(CAVerifierServerApplicationContractsModule))]
public class CAVerifierServerGrainsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<VerifierCodeOptions>(configuration.GetSection("VerifierCode"));
        Configure<VerifierAccountOptions>(configuration.GetSection("verifierAccount"));

    }
}