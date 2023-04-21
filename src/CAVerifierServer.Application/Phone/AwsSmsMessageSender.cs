using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using CAVerifierServer.CustomException;
using CAVerifierServer.Options;
using CAVerifierServer.VerifyCodeSender;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Sms;

namespace CAVerifierServer.Phone;

public class AwsSmsMessageSender : ISMSServiceSender
{
    public string ServiceName => "AWS";
    private readonly ILogger<AwsSmsMessageSender> _logger;
    private readonly VerifierInfoOptions _verifierInfoOptions;
    private readonly AwssmsMessageOptions _awssmsMessageOptions;
    private readonly AmazonSimpleNotificationServiceClient _amazonSimpleNotificationServiceClient;
    private const string SuccessMark = "2";
    private const string PhoneNumReplacement = "$1****$2";
    private readonly Regex _regex;


    public AwsSmsMessageSender(ILogger<AwsSmsMessageSender> logger, IOptions<VerifierInfoOptions> verifierInfoOptions,
        IOptions<AwssmsMessageOptions> smsMessageOptions)
    {
        _logger = logger;
        _regex = new Regex("(.{6}).*(.{4})");
        _awssmsMessageOptions = smsMessageOptions.Value;
        _verifierInfoOptions = verifierInfoOptions.Value;
        _amazonSimpleNotificationServiceClient = new AmazonSimpleNotificationServiceClient(
            _awssmsMessageOptions.AwsAccessKeyId, _awssmsMessageOptions.AwsSecretAccessKeyId,
            RegionEndpoint.GetBySystemName(_awssmsMessageOptions.SystemName));
    }

    private async Task SendTextMessageAsync(SmsMessage smsMessage)
    {
        if (string.IsNullOrEmpty(smsMessage.PhoneNumber) || string.IsNullOrEmpty(smsMessage.Text))
        {
            _logger.LogError("PhoneNum or message text is invalidate");
            return;
        }

        // Now actually send the message.
        var request = new PublishRequest
        {
            Message = SMSMessageBodyBuilder.BuildBodyTemplate(_verifierInfoOptions.Name, smsMessage.Text),
            PhoneNumber = smsMessage.PhoneNumber
        };
        try
        {
            _logger.LogDebug("AWS SMS Service sending SMSMessage to {phoneNum}",
                _regex.Replace(smsMessage.PhoneNumber, PhoneNumReplacement));
            var response = await _amazonSimpleNotificationServiceClient.PublishAsync(request);
            var isSuccess = Convert.ToInt32(response.HttpStatusCode).ToString().StartsWith(SuccessMark);
            if (!isSuccess)
            {
                _logger.LogError(
                    "AWS SMS Service sending SMSMessage failed to {phoneNum}, ResponseCode is {statusCode}",
                    _regex.Replace(smsMessage.PhoneNumber, PhoneNumReplacement), response.HttpStatusCode);
                throw new SmsSenderFailedException("AWS SMS Service sending SMSMessage failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AWS SMS Service Sending message error : {ex}", ex.Message);
            throw ex;
        }
    }


    public async Task SendAsync(SmsMessage smsMessage)
    {
        await SendTextMessageAsync(smsMessage);
    }
}