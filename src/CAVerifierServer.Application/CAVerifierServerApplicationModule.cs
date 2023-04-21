using CAVerifierServer.AccountAction;
using CAVerifierServer.Email;
using CAVerifierServer.Grains;
using CAVerifierServer.Options;
using CAVerifierServer.VerifyCodeSender;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Account;
using Volo.Abp.AutoMapper;
using Volo.Abp.Emailing;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace CAVerifierServer;

[DependsOn(
    typeof(CAVerifierServerDomainModule),
    typeof(AbpAccountApplicationModule),
    typeof(CAVerifierServerApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule),
    typeof(CAVerifierServerGrainsModule)
)]
public class CAVerifierServerApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<CAVerifierServerApplicationModule>(); });
        var configuration = context.Services.GetConfiguration();
        Configure<ChainOptions>(configuration.GetSection("Chains"));
        Configure<WhiteListExpireTimeOptions>(configuration.GetSection("WhiteListExpireTimeOptions"));
        Configure<VerifierInfoOptions>(configuration.GetSection("VerifierInfoOption"));
        Configure<AwsEmailOptions>(configuration.GetSection("awsEmail"));
        context.Services.AddSingleton<IEmailSender, AwsEmailSender>();
        context.Services.AddSingleton<IVerifyCodeSender, EmailVerifyCodeSender>();
        context.Services.AddSingleton<IVerifyCodeSender, PhoneVerifyCodeSender>();
    }
}