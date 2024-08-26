using System;

namespace CAVerifierServer.Account;

public class SecondaryEmailVerificationInput
{
    public string SecondaryEmail { get; set; }
    
    public Guid VerifierSessionId { get; set; }
}