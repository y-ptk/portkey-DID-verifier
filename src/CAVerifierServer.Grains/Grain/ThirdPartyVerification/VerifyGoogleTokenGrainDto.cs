using CAVerifierServer.Account;

namespace CAVerifierServer.Grains.Grain;

public class VerifyGoogleTokenGrainDto : VerifierCodeDto
{
    public GoogleUserExtraInfo GoogleUserExtraInfo { get; set; }
}