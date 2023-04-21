using CAVerifierServer.Email;
using CAVerifierServer.Grains.Options;
using CAVerifierServer.Orleans.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace CAVerifierServer;

[DependsOn(
    typeof(CAVerifierServerApplicationModule),
    typeof(CAVerifierServerDomainTestModule),
    typeof(CAVerifierServerOrleansTestBaseModule)
    )]
public class CAVerifierServerApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<VerifierAccountOptions>(o =>
        {
            o.PrivateKey = "XXXXXXX";
            o.Address = "XXXXXXX";
        });

        context.Services.Configure<VerifierInfoOptions>(o =>
        {
            o.Name = "Verifier-001";
            o.CaServerUrl = "http://127.0.0.1:5577";
        });

        context.Services.Configure<AwsEmailOptions>(o =>
        {
            o.From = "XXXX@XXXX.com";
            o.FromName = "XXXXXX";
            o.SmtpUsername = "XXXXXXX";
            o.SmtpPassword = "XXXXXXX";
            o.ConfigSet = "";
            o.Host = "email-smtp.ap-northeast-1.amazonaws.com";
            o.Port = 587;
            o.Image = "https://127.0.0.1.png";
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
