using CAVerifierServer.MongoDB;
using Volo.Abp.Modularity;

namespace CAVerifierServer;

[DependsOn(
    typeof(CAVerifierServerMongoDbTestModule)
    )]
public class CAVerifierServerDomainTestModule : AbpModule
{

}
