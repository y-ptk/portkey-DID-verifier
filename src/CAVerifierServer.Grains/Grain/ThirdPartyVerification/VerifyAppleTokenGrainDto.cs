using CAVerifierServer.Account;

namespace CAVerifierServer.Grains.Grain;

public class VerifyAppleTokenGrainDto : VerifierCodeDto
{
    public AppleUserExtraInfo AppleUserExtraInfo { get; set; }
}