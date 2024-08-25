namespace CAVerifierServer.Account;

public class SecondaryEmailVerificationInput
{
    public string SecondaryEmail { get; set; }
    
    public string VerifierSessionId { get; set; }
}