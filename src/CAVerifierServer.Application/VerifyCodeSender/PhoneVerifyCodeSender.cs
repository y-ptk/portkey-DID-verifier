using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CAVerifierServer.AccountAction;
using Microsoft.Extensions.Logging;

namespace CAVerifierServer.VerifyCodeSender;


public class PhoneVerifyCodeSender : IVerifyCodeSender
{
    public string Type => "PhoneNumber";
    private readonly Regex _regex;
    private readonly ILogger<PhoneVerifyCodeSender> _logger;

    public PhoneVerifyCodeSender(ILogger<PhoneVerifyCodeSender> logger)
    {
        _logger = logger;
        _regex = new Regex(CAVerifierServerApplicationConsts.PhoneRegex);
    }
    
    public async Task SendCodeByGuardianIdentifierAsync(string guardianIdentifier, string code)
    {
        _logger.LogWarning("The PhoneSend To Be Develop");
    }

    public bool ValidateGuardianIdentifier(string guardianIdentifier)
    {
       return !string.IsNullOrWhiteSpace(guardianIdentifier) && _regex.IsMatch(guardianIdentifier);
    }
}