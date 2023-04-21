using CAVerifierServer.Email;
using CAVerifierServer.Grain.Tests;
using CAVerifierServer.Grains.Options;
using CAVerifierServer.Options;
using CAVerifierServer.Phone;
using CAVerifierServer.VerifyCodeSender;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
using NSubstitute.Extensions;
using Volo.Abp.Emailing;
using Volo.Abp.Modularity;
using Volo.Abp.Sms;

namespace CAVerifierServer;

[DependsOn(
    typeof(CAVerifierServerApplicationModule),
    typeof(CAVerifierServerDomainTestModule),
    typeof(CAVerifierServerGrainTestModule)
    )]
public class CAVerifierServerApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<VerifierAccountOptions>(o =>
        {
            o.PrivateKey = "XXXXXXXX";
            o.Address = "XXXXXXXX";
        });
        
        context.Services.AddSingleton<IEmailSender, NullEmailSender>();
        context.Services.AddSingleton<ISMSServiceSender, AwsSmsMessageSender>();
        context.Services.AddSingleton<ISMSServiceSender, TelesignSmsMessageSender>();
        context.Services.AddSingleton<ISmsSender, NullSmsSender>();
        context.Services.Configure<AwssmsMessageOptions>(o =>
        {
            o.SystemName = "abc";
            o.AwsAccessKeyId = "qbc";
            o.AwsSecretAccessKeyId = "abc";
        });
        context.Services.Configure<TelesignSMSMessageOptions>(o =>
        {
            o.Type = "qbc";
            o.ApiKey = "qbc";
            o.CustomerId = "qbc";
        });
        
        context.Services.Configure<VerifierInfoOptions>(o =>
        {
            o.Name = "Verifier-001";
            o.CaServerUrl = "http://127.0.0.1:5577";
        });
        
        base.ConfigureServices(context);
    }

}
