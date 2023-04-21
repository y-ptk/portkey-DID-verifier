using System.Threading.Tasks;
using Moq;
using Volo.Abp.Emailing;

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


}