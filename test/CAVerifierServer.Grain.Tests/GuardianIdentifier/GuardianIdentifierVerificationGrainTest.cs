using AElf;
using CAVerifierServer.Account;
using CAVerifierServer.Grains;
using CAVerifierServer.Grains.Grain;
using CAVerifierServer.VerifyCodeSender;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;
using Shouldly;
using Volo.Abp.Timing;
using Xunit;

namespace CAVerifierServer.Grain.Tests.GuardianIdentifier;

[Collection(ClusterCollection.Name)]
public class GuardianIdentifierVerificationGrainTest : CAVerifierServerGrainTestBase
{
    private const string DefaultEmailAddress = "sam@XXX.com";
    private const string DefaultType = "Email";
    private const string DefaultCode = "123456";
    private readonly IEnumerable<IVerifyCodeSender> _verifyCodeSender;
    private const string Email = "sam@XXXX.com";

    public GuardianIdentifierVerificationGrainTest()
    {
        var clock = GetRequiredService<IClock>();
    }

    [Fact]
    public async Task GetVerifyCode_Success_Test()
    {
        var verifierSessionId = Guid.NewGuid();
        var grain = Cluster.Client.GetGrain<IGuardianIdentifierVerificationGrain>(DefaultType);
        var result = await grain.GetVerifyCodeAsync(new SendVerificationRequestInput
        {
            Type = DefaultType,
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = verifierSessionId
        });
        result.Success.ShouldBe(true);
        result.Data.VerifierCode.Length.ShouldBe(6);
    }

    [Fact]
    public async Task GetVerifyCode_OverFrequencyLimit_Test()
    {
        var verifierSessionId = Guid.NewGuid();
        var grain = Cluster.Client.GetGrain<IGuardianIdentifierVerificationGrain>(DefaultType);
        await grain.GetVerifyCodeAsync(new SendVerificationRequestInput
        {
            Type = DefaultType,
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = verifierSessionId
        });
        var result = await grain.GetVerifyCodeAsync(new SendVerificationRequestInput
        {
            Type = DefaultType,
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = Guid.NewGuid()
        });

        result.Success.ShouldBe(false);
        result.Message.ShouldBe("The interval between sending two verification codes is less than 60s");
    }

    [Fact]
    public async Task GetVerifyCode_SameVerifierSessionId_Test()
    {
        var verifierSessionId = Guid.NewGuid();
        var grain = Cluster.Client.GetGrain<IGuardianIdentifierVerificationGrain>(DefaultType);
        var result = await grain.GetVerifyCodeAsync(new SendVerificationRequestInput
        {
            Type = DefaultType,
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = verifierSessionId
        });
        result.Data.VerifierCode.Length.ShouldBe(6);

        Change_Clock_Now(2);

        var resultDto = await grain.GetVerifyCodeAsync(new SendVerificationRequestInput
        {
            Type = DefaultType,
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = verifierSessionId
        });
        resultDto.Success.ShouldBe(true);
        resultDto.Data.VerifierCode.ShouldNotBe(result.Data.VerifierCode);
    }


    [Fact]
    public async Task VerifyAndCreateSignature_Success_Test()
    {
        var verifierSessionId = Guid.NewGuid();
        var grain = Cluster.Client.GetGrain<IGuardianIdentifierVerificationGrain>(DefaultType);
        var result = await grain.GetVerifyCodeAsync(new SendVerificationRequestInput
        {
            Type = DefaultType,
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = verifierSessionId
        });

        var salt = verifierSessionId.ToString().Replace("-", "");
        var hash = HashHelper.ComputeFrom(salt + HashHelper.ComputeFrom(DefaultEmailAddress).ToHex()).ToHex();
        var signatureAsyncResult = await grain.VerifyAndCreateSignatureAsync(new VerifyCodeInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            Code = result.Data.VerifierCode,
            VerifierSessionId = verifierSessionId,
            Salt = salt,
            GuardianIdentifierHash = hash
        });
        signatureAsyncResult.Success.ShouldBe(true);
    }

    [Fact]
    public async Task VerifyAndCreateSignature_AlreadyVerified_Test()
    {
        var verifierSessionId = Guid.NewGuid();
        var grain = Cluster.Client.GetGrain<IGuardianIdentifierVerificationGrain>(DefaultType);
        var result = await grain.GetVerifyCodeAsync(new SendVerificationRequestInput
        {
            Type = DefaultType,
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = verifierSessionId
        });

        var salt = verifierSessionId.ToString().Replace("-", "");
        var hash = HashHelper.ComputeFrom(salt + HashHelper.ComputeFrom(DefaultEmailAddress).ToHex()).ToHex();
        var signatureAsyncResult = await grain.VerifyAndCreateSignatureAsync(new VerifyCodeInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            Code = result.Data.VerifierCode,
            VerifierSessionId = verifierSessionId,
            Salt = salt,
            GuardianIdentifierHash = hash
        });
        signatureAsyncResult.Success.ShouldBe(true);

        var dto = await grain.VerifyAndCreateSignatureAsync(new VerifyCodeInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            Code = result.Data.VerifierCode,
            VerifierSessionId = verifierSessionId,
            Salt = salt,
            GuardianIdentifierHash = hash
        });
        dto.Success.ShouldBe(false);
        dto.Message.ShouldBe(Error.Message[Error.Verified]);
    }


    [Fact]
    public async Task VerifyAndCreateSignature_InvalidLoginGuardianIdentifier_Test()
    {
        var verifierSessionId = Guid.NewGuid();
        var grain = Cluster.Client.GetGrain<IGuardianIdentifierVerificationGrain>(DefaultType);
        var result = await grain.GetVerifyCodeAsync(new SendVerificationRequestInput
        {
            Type = DefaultType,
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = verifierSessionId
        });

        var salt = verifierSessionId.ToString().Replace("-", "");
        var hash = HashHelper.ComputeFrom(salt + HashHelper.ComputeFrom(DefaultEmailAddress).ToHex()).ToHex();

        var newGrain = Cluster.Client.GetGrain<IGuardianIdentifierVerificationGrain>(Email);
        var signatureAsyncResult = await newGrain.VerifyAndCreateSignatureAsync(new VerifyCodeInput
        {
            GuardianIdentifier = Email,
            Code = result.Data.VerifierCode,
            VerifierSessionId = verifierSessionId,
            Salt = salt,
            GuardianIdentifierHash = hash
        });
        signatureAsyncResult.Success.ShouldBe(false);
        signatureAsyncResult.Message.ShouldBe(Error.Message[Error.InvalidLoginGuardianIdentifier]);
    }


    [Fact]
    public async Task VerifyAndCreateSignature_InvalidVerifierSessionId_Test()
    {
        var verifierSessionId = Guid.NewGuid();
        var grain = Cluster.Client.GetGrain<IGuardianIdentifierVerificationGrain>(DefaultType);
        var result = await grain.GetVerifyCodeAsync(new SendVerificationRequestInput
        {
            Type = DefaultType,
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = verifierSessionId
        });

        var salt = verifierSessionId.ToString().Replace("-", "");
        var hash = HashHelper.ComputeFrom(salt + HashHelper.ComputeFrom(DefaultEmailAddress).ToHex()).ToHex();
        var signatureAsyncResult = await grain.VerifyAndCreateSignatureAsync(new VerifyCodeInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            Code = result.Data.VerifierCode,
            VerifierSessionId = Guid.NewGuid(),
            Salt = salt,
            GuardianIdentifierHash = hash
        });
        signatureAsyncResult.Message.ShouldBe(Error.Message[Error.InvalidVerifierSessionId]);
    }


    [Fact]
    public async Task VerifyAndCreateSignature_Timeout_Test()
    {
        var verifierSessionId = Guid.NewGuid();
        var grain = Cluster.Client.GetGrain<IGuardianIdentifierVerificationGrain>(DefaultType);
        var result = await grain.GetVerifyCodeAsync(new SendVerificationRequestInput
        {
            Type = DefaultType,
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = verifierSessionId
        });
        Change_Clock_Now(3);
        var salt = verifierSessionId.ToString().Replace("-", "");
        var hash = HashHelper.ComputeFrom(salt + HashHelper.ComputeFrom(DefaultEmailAddress).ToHex()).ToHex();
        var signatureAsyncResult = await grain.VerifyAndCreateSignatureAsync(new VerifyCodeInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            Code = result.Data.VerifierCode,
            VerifierSessionId = verifierSessionId,
            Salt = salt,
            GuardianIdentifierHash = hash
        });
        signatureAsyncResult.Message.ShouldBe(Error.Message[Error.Timeout]);
    }

    [Fact]
    public async Task VerifyAndCreateSignature_TooManyRetries_Test()
    {
        var verifierSessionId = Guid.NewGuid();
        var grain = Cluster.Client.GetGrain<IGuardianIdentifierVerificationGrain>(DefaultType);
        await grain.GetVerifyCodeAsync(new SendVerificationRequestInput
        {
            Type = DefaultType,
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = verifierSessionId
        });
        var salt = verifierSessionId.ToString().Replace("-", "");
        var hash = HashHelper.ComputeFrom(salt + HashHelper.ComputeFrom(DefaultEmailAddress).ToHex()).ToHex();
        var signatureAsyncResult = await grain.VerifyAndCreateSignatureAsync(new VerifyCodeInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            Code = DefaultCode,
            VerifierSessionId = verifierSessionId,
            Salt = salt,
            GuardianIdentifierHash = hash
        });
        signatureAsyncResult.Success.ShouldBe(false);
        signatureAsyncResult.Message.ShouldBe(Error.Message[Error.WrongCode]);
        var resultDto = await grain.VerifyAndCreateSignatureAsync(new VerifyCodeInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            Code = DefaultCode,
            VerifierSessionId = verifierSessionId,
            Salt = salt,
            GuardianIdentifierHash = hash
        });
        resultDto.Success.ShouldBe(false);
        resultDto.Message.ShouldBe(Error.Message[Error.WrongCode]);

        var result = await grain.VerifyAndCreateSignatureAsync(new VerifyCodeInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            Code = DefaultCode,
            VerifierSessionId = verifierSessionId,
            Salt = salt,
            GuardianIdentifierHash = hash
        });
        result.Success.ShouldBe(false);
        result.Message.ShouldBe(Error.Message[Error.TooManyRetries]);
    }

    private void Change_Clock_Now(int offset)
    {
        foreach (var silo in Cluster.Silos)
        {
            ((MockClock)((InProcessSiloHandle)silo).SiloHost.Services.GetRequiredService<IClock>()).SetOffset(offset);
        }
    }
}