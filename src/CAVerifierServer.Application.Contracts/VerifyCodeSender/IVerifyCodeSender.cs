using System.Threading.Tasks;

namespace CAVerifierServer.VerifyCodeSender;

public interface IVerifyCodeSender
{
    string Type { get; }

    Task SendCodeByGuardianIdentifierAsync(string guardianIdentifier, string code);
    
    bool ValidateGuardianIdentifier(string guardianIdentifier);
    
    
}