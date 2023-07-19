using System.Collections.Generic;
using System.Threading.Tasks;
using CAVerifierServer.CustomException;
using CAVerifierServer.Options;
using CAVerifierServer.VerifyCodeSender;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp.Sms;

namespace CAVerifierServer.MsgSender;

public partial class VerifierCodeSmsSenderTests
{
    private IOptions<SmsServiceOptions> GetSmsServiceOptions()
    {
        var smsServiceDic = new Dictionary<string, SmsServiceOption>();
        smsServiceDic.Add("AWS", new SmsServiceOption
        {
            SupportingCountriesRadio = new Dictionary<string, int>
            {
                { "CN", 1 }
            },
        });
        smsServiceDic.Add("Telesign", new SmsServiceOption
        {
            SupportingCountriesRadio = new Dictionary<string, int>
            {
                { "CN", 2 }
            },
        });
        smsServiceDic.Add("Twilio", new SmsServiceOption
        {
            SupportingCountriesRadio = new Dictionary<string, int>
            {
                { "CN", 3 }
            },
        });
        smsServiceDic.Add("MockSmsServiceSender", new SmsServiceOption
        {
            SupportingCountriesRadio = new Dictionary<string, int>
            {
                { "CN", 4 }
            },
        });
        return new OptionsWrapper<SmsServiceOptions>(
            new SmsServiceOptions
            {
                SmsServiceInfos = smsServiceDic
            });
    }

    private ISMSServiceSender GetMockSmsServiceSender()
    {
        var mockSmsSender = new Mock<ISMSServiceSender>();
        mockSmsSender.Setup(o => o.ServiceName).Returns("MockSmsServiceSender");
        mockSmsSender.Setup(o => o.SendAsync(It.IsAny<SmsMessage>()))
            .Returns(Task.CompletedTask);
        return mockSmsSender.Object;
    }
}