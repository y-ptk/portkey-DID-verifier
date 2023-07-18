using System.Collections.Generic;
using System.Threading.Tasks;
using CAVerifierServer.CustomException;
using CAVerifierServer.Options;
using CAVerifierServer.VerifyCodeSender;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp.Emailing;
using Volo.Abp.Sms;

namespace CAVerifierServer.MsgSender;

public partial class VerifierCodeSenderTest
{
    private IEmailSender GetMockEmailSender()
    {
        var mockEmailSender = new Mock<IEmailSender>();
        mockEmailSender.Setup(o => o.QueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.CompletedTask);
        return mockEmailSender.Object;
    }

    private ISmsSender GetMockSmsSender()
    {
        var mockSmsSender = new Mock<ISmsSender>();
        mockSmsSender.Setup(o => o.SendAsync(It.IsAny<SmsMessage>()))
            .Returns(Task.CompletedTask);
        return mockSmsSender.Object;
    }

    private ISMSServiceSender GetMockSmsServiceSender()
    {
        var mockSmsSender = new Mock<ISMSServiceSender>();
        mockSmsSender.Setup(o => o.ServiceName).Returns("MockSmsServiceSender");
        mockSmsSender.Setup(o => o.SendAsync(It.IsAny<SmsMessage>()))
            .Returns((SmsMessage smsMessage) =>
            {
                if (smsMessage.PhoneNumber == FakeFailedPhoneNum)
                {
                    throw new SmsSenderFailedException("SendSmsFailed");
                }

                return Task.CompletedTask;
            });

        return mockSmsSender.Object;
    }

    private IOptions<SmsServiceOptions> GetSmsServiceOptions()
    {
        var smsServiceDic = new Dictionary<string, SmsServiceOption>();
        smsServiceDic.Add("MockSmsServiceSender", new SmsServiceOption
        {
            SupportingCountriesRadio = new Dictionary<string, int>
            {
                { "CN", 1 }
            },
        });
        smsServiceDic.Add("MockSmsServiceSender2", new SmsServiceOption
        {
            SupportingCountriesRadio = new Dictionary<string, int>
            {
                { "CN", 1 }
            },
        });
        return new OptionsWrapper<SmsServiceOptions>(
            new SmsServiceOptions
            {
                SmsServiceInfos = smsServiceDic
            });
    }
}