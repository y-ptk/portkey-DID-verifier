using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using CAVerifierServer.Options;
using CAVerifierServer.VerifyCodeSender;
using Microsoft.Extensions.Options;
using Moq;
using NSubstitute;
using Volo.Abp.Emailing;
using Volo.Abp.Sms;

namespace CAVerifierServer.SmsSender;

public partial class SmsSenderTest
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
            .Returns(Task.CompletedTask);
        return mockSmsSender.Object;
    }

    private IOptions<SmsServiceOptions> GetSmsServiceOptions()
    {
        var smsServiceDic = new Dictionary<string, SmsServiceOption>();
        smsServiceDic.Add("MockSmsServiceSender", new SmsServiceOption
        {
            IsEnable = true,
            Ratio = 1
        });
        return new OptionsWrapper<SmsServiceOptions>(
            new SmsServiceOptions
            {
                SmsServiceInfos = smsServiceDic
            });
    }

    private IOptions<AwsEmailOptions> GetAwsEmailOptions()
    {
        return new OptionsWrapper<AwsEmailOptions>(
            new AwsEmailOptions
            {
                From = "sam@XXXXX.com",
                ConfigSet = "MockConfigSet",
                FromName = "MockName",
                Host = "MockHost",
                Image = "MockImage",
                Port = 8000,
                SmtpUsername = "MockUsername",
                SmtpPassword = "MockPassword"
            });
    }
}