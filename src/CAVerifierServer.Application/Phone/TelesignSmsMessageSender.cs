using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAVerifierServer.Options;
using CAVerifierServer.VerifyCodeSender;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TelesignEnterprise;
using Volo.Abp.Sms;

namespace CAVerifierServer.Phone;

public class TelesignSmsMessageSender : ISMSServiceSender
{
    public string ServiceName => "Telesign";
    private readonly ILogger<TelesignSmsMessageSender> _logger;
    private readonly VerifierInfoOptions _verifierInfoOptions;
    private readonly TelesignSMSMessageOptions _telesignSMSMessageOptions;
    private readonly MessagingClient _messagingClient;

    public TelesignSmsMessageSender(ILogger<TelesignSmsMessageSender> logger,
        IOptions<VerifierInfoOptions> verifierInfoOptions,
        IOptions<TelesignSMSMessageOptions> telesignSmsMessageOptions)
    {
        _logger = logger;
        _telesignSMSMessageOptions = telesignSmsMessageOptions.Value;
        _verifierInfoOptions = verifierInfoOptions.Value;
        _messagingClient =
            new MessagingClient(_telesignSMSMessageOptions.CustomerId, _telesignSMSMessageOptions.ApiKey);
    }

    private async Task SendTextMessageAsync(SmsMessage smsMessage)
    {
        if (string.IsNullOrEmpty(smsMessage.PhoneNumber) || string.IsNullOrEmpty(smsMessage.Text))
        {
            _logger.LogError("PhoneNum or message text is invalidate");
            return;
        }

        var phoneNumber = smsMessage.PhoneNumber;
        var message = SMSMessageBodyBuilder.BuildBodyTemplate(_verifierInfoOptions.Name, smsMessage.Text);
        try
        {
            _logger.LogDebug("Telesign SMS Service sending SMSMessage to {phoneNum}",
                smsMessage.PhoneNumber);
            var response = await _messagingClient.MessageAsync(phoneNumber, message, _telesignSMSMessageOptions.Type);
            if (!response.OK)
            {
                _logger.LogError(
                    "Telesign SMS Service sending SMSMessage failed to {phoneNum}",
                    smsMessage.PhoneNumber);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telesign SMS Service sending message error : {ex}", ex.Message);
            throw ex;
        }
    }

    public async Task SendAsync(SmsMessage smsMessage)
    {
        await SendTextMessageAsync(smsMessage);
    }
}