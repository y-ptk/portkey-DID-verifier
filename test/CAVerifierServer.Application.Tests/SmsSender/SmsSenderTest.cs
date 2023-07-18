using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAVerifierServer.VerifyCodeSender;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp.Sms;
using Xunit;

namespace CAVerifierServer.SmsSender;

[Collection(CAVerifierServerTestConsts.CollectionDefinitionName)]
public partial class SmsSenderTest : CAVerifierServerApplicationTestBase
{
    private readonly IEnumerable<ISMSServiceSender> smsServiceSender;
    private const string AWS = "AWS";
    private const string Telesign = "Telesign";
    private const string UnSupportType = "InvalidateType";
    private const string FakePhoneNum = "+861234567890";
    private const string FakeCode = "123456";


    public SmsSenderTest()
    {
        smsServiceSender = GetRequiredService<IEnumerable<ISMSServiceSender>>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(GetAwsEmailOptions());
        services.AddSingleton(GetSmsTemplateOptions());
    }

    [Fact]
    public async Task SendCodeByGuardianIdentifier_TypeTest()
    {
        var awsSmsSender = smsServiceSender.FirstOrDefault(o => o.ServiceName == AWS);
        awsSmsSender.ServiceName.ShouldBe(AWS);
        var telesignSmsSender = smsServiceSender.FirstOrDefault(o => o.ServiceName == Telesign);
        telesignSmsSender.ServiceName.ShouldBe(Telesign);
        var unSupportSmsSender = smsServiceSender.FirstOrDefault(o => o.ServiceName == UnSupportType);
        unSupportSmsSender.ShouldBeNull();
        var smsMessage = new SmsMessage(FakePhoneNum, FakeCode);
        try
        {
            await telesignSmsSender.SendAsync(smsMessage);
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }

        try
        {
            await awsSmsSender.SendAsync(smsMessage);
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }
}