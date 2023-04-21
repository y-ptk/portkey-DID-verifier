using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace CAVerifierServer.AccountAction;

public interface IVerifyCodeSender
{
    string Type { get; }

    Task SendCodeByGuardianIdentifierAsync(string guardianIdentifier, string code);
    
    bool ValidateGuardianIdentifier(string guardianIdentifier);
    
    
}