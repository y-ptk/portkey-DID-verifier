using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAVerifierServer.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Sms;

namespace CAVerifierServer.VerifyCodeSender;

public class PhoneVerifyCodeSender : IVerifyCodeSender
{
    public string Type => "Phone";
    private readonly ILogger<PhoneVerifyCodeSender> _logger;
    private readonly IEnumerable<ISMSServiceSender> _smsServiceSender;
    private readonly SMSServiceRatioOptions _smsServiceRatioOptions;

    public PhoneVerifyCodeSender(ILogger<PhoneVerifyCodeSender> logger,
        IEnumerable<ISMSServiceSender> smsServiceSender,
        IOptions<SMSServiceRatioOptions> smsServiceRatioOptions)
    {
        _logger = logger;
        _smsServiceSender = smsServiceSender;
        _smsServiceRatioOptions = smsServiceRatioOptions.Value;
    }

    public async Task SendCodeByGuardianIdentifierAsync(string guardianIdentifier, string code)
    {
        var maxRatioService = _smsServiceRatioOptions.SmsServiceRatioDic.MaxBy(k => k.Value).Key;
        var serviceProviderSelector = _smsServiceSender.FirstOrDefault(v => v.ServiceName == maxRatioService);
        if (serviceProviderSelector == null)
        {
            _logger.LogError("Can not find sms service provider {serviceName}", maxRatioService);
            return;
        }

        _logger.LogDebug("choose sms service provider : {serviceName}", serviceProviderSelector.ServiceName);
        await serviceProviderSelector.SendAsync(new SmsMessage(guardianIdentifier, code));
    }

    public bool ValidateGuardianIdentifier(string guardianIdentifier)
    {
        return !string.IsNullOrWhiteSpace(guardianIdentifier);
    }
}