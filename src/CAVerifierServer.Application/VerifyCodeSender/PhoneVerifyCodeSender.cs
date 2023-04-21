using System;
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
    private readonly SmsServiceOptions _smsServiceOptions;
    private const string ChineseRegionNum = "+86";

    public PhoneVerifyCodeSender(ILogger<PhoneVerifyCodeSender> logger,
        IEnumerable<ISMSServiceSender> smsServiceSender,
        IOptions<SmsServiceOptions> smsServiceOptions)
    {
        _logger = logger;
        _smsServiceSender = smsServiceSender;
        _smsServiceOptions = smsServiceOptions.Value;
    }

    public async Task SendCodeByGuardianIdentifierAsync(string guardianIdentifier, string code)
    {
        Dictionary<string, int> smsServiceDic;
        if (guardianIdentifier.StartsWith(ChineseRegionNum))
        {
            smsServiceDic = _smsServiceOptions.SmsServiceInfos.Where(o => o.Value.IsEnable)
                .OrderByDescending(k => k.Value.Ratio)
                .ToDictionary(o => o.Key, o => o.Value.Ratio);
        }
        else
        {
            smsServiceDic = _smsServiceOptions.SmsServiceInfos
                .OrderByDescending(k => k.Value.Ratio)
                .ToDictionary(o => o.Key, o => o.Value.Ratio);
        }

        if (smsServiceDic.Count == 0)
        {
            _logger.LogError("No sms service provider is enable");
            return;
        }

        var failedServicesCount = 0;
        foreach (var smsServiceSenderName in smsServiceDic.Keys)
        {
            var smsServiceSender = _smsServiceSender.FirstOrDefault(o => o.ServiceName == smsServiceSenderName);
            if (smsServiceSender == null)
            {
                _logger.LogError("Can not find sms service provider {serviceName}", smsServiceDic.FirstOrDefault().Key);
                return;
            }

            try
            {
                _logger.LogDebug("Choose sms service provider is : {serviceName}", smsServiceSender.ServiceName);
                await smsServiceSender.SendAsync(new SmsMessage(guardianIdentifier, code));
                break;
            }
            catch (Exception e)
            {
                _logger.LogDebug("{serviceName} sending sms failed : Error:{e}", smsServiceSender.ServiceName,
                    e.Message);
                failedServicesCount += 1;
                if (failedServicesCount < smsServiceDic.Count)
                {
                    continue;
                }

                _logger.LogError("All sms service provider sending sms failed");
                throw e;
            }
        }
    }


    public bool ValidateGuardianIdentifier(string guardianIdentifier)
    {
        return !string.IsNullOrWhiteSpace(guardianIdentifier);
    }
}