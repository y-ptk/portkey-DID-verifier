using System;
using System.Threading.Tasks;
using AElf;
using Shouldly;
using Xunit;

namespace CAVerifierServer.Account;

/* This is just an example test class.
 * Normally, you don't test code of the modules you are using
 * (like IIdentityUserAppService here).
 * Only test your own application services.
 */
[Collection(CAVerifierServerTestConsts.CollectionDefinitionName)]
public class AccountAppServiceTests : CAVerifierServerApplicationTestBase
{
    private const string DefaultEmailAddress = "sam@XXXXX.com";
    private readonly IAccountAppService _accountAppService;
    private const string Type = "Email";
    private const string Code = "123456";
    private const string InvalidType = "ErrorType";
    private const string InvalidEmail = "1234567";

    public AccountAppServiceTests()
    {
        _accountAppService = GetRequiredService<IAccountAppService>();
    }


    [Fact]
    public async Task SendVerificationRequest_Success_Test()
    {
        var verifierSessionId = Guid.NewGuid();
        //success
        var result = await _accountAppService.SendVerificationRequestAsync(new SendVerificationRequestInput
        {
            Type = Type,
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = verifierSessionId
        });
        var randomCode = RandomNumProvider.GetCode(6);
        randomCode.Length.ShouldBe(6);
        result.Success.ShouldBe(true);
        result.Data.VerifierSessionId.ShouldBe(verifierSessionId);
    }

    [Fact]
    public async Task SendVerificationRequest_OverFrequencyLimit_Test()
    {
        var verifierSessionId = Guid.NewGuid();
        //success
        var result = await _accountAppService.SendVerificationRequestAsync(new SendVerificationRequestInput
        {
            Type = Type,
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = verifierSessionId
        });
        var randomCode = RandomNumProvider.GetCode(6);
        randomCode.Length.ShouldBe(6);
        result.Success.ShouldBe(true);
        result.Data.VerifierSessionId.ShouldBe(verifierSessionId);

        var dto = await _accountAppService.SendVerificationRequestAsync(new SendVerificationRequestInput
        {
            Type = Type,
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = verifierSessionId
        });

        dto.Success.ShouldBe(false);
        dto.Message.ShouldBe("The interval between sending two verification codes is less than 60s");
    }


    [Fact]
    public async Task SendVerificationRequest_UnsupportedType_Test()
    {
        var result = await _accountAppService.SendVerificationRequestAsync(new SendVerificationRequestInput
        {
            Type = InvalidType,
            VerifierSessionId = Guid.NewGuid(),
            GuardianIdentifier = DefaultEmailAddress
        });
        result.Success.ShouldBe(false);
        result.Message.ShouldBe(Error.Message[Error.Unsupported]);
    }

    [Fact]
    public async Task SendVerificationRequest_Register_InvalidEmailInput_Test()
    {
        var result = await _accountAppService.SendVerificationRequestAsync(new SendVerificationRequestInput
        {
            GuardianIdentifier = InvalidEmail,
            VerifierSessionId = Guid.NewGuid(),
            Type = Type
        });
        result.Success.ShouldBe(false);
        result.Message.ShouldBe(Error.Message[Error.InvalidGuardianIdentifierInput]);
    }

    [Fact]
    public async Task SendVerificationRequest_InputIsNullOrEmpty_Test()
    {
        var result = await _accountAppService.SendVerificationRequestAsync(new SendVerificationRequestInput
        {
            VerifierSessionId = Guid.NewGuid(),
            Type = Type
        });
        result.Success.ShouldBe(false);
        result.Message.ShouldBe(Error.Message[Error.InvalidGuardianIdentifierInput]);

        var resultDto = await _accountAppService.SendVerificationRequestAsync(new SendVerificationRequestInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            Type = Type
        });
        resultDto.Success.ShouldBe(true);
        resultDto.Data.VerifierSessionId.ShouldBe(Guid.Empty);

        var dto = await _accountAppService.SendVerificationRequestAsync(new SendVerificationRequestInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = Guid.NewGuid()
        });
        dto.Success.ShouldBe(false);
        dto.Message.ShouldBe(Error.Message[Error.Unsupported]);
    }


    [Fact]
    public async Task VerifyCode_VerifierSessionIdNotExist_Test()
    {
        var id = await SendVerificationRequest();
        var salt = Guid.NewGuid().ToString().Replace("-", "");
        var hash = HashHelper.ComputeFrom(salt + HashHelper.ComputeFrom(DefaultEmailAddress).ToHex()).ToHex();
        var result = await _accountAppService.VerifyCodeAsync(new VerifyCodeInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = Guid.NewGuid(),
            Code = Code,
            GuardianIdentifierHash = hash
        });
        result.Success.ShouldBe(false);
        result.Message.ShouldBe("Input is null or empty");
    }


    [Fact]
    public async Task VerifyCode_NotMatch_Test()
    {
        var salt = Guid.NewGuid().ToString().Replace("-", "");
        var hash = HashHelper.ComputeFrom(salt + HashHelper.ComputeFrom(DefaultEmailAddress).ToHex()).ToHex();
        var id = await SendVerificationRequest();
        var unMatchEmail = "eric@XXXX.com";
        var result = await _accountAppService.VerifyCodeAsync(new VerifyCodeInput
        {
            GuardianIdentifier = unMatchEmail,
            Code = Code,
            VerifierSessionId = id,
            Salt = salt,
            GuardianIdentifierHash = hash
        });
        result.Success.ShouldBe(false);
        result.Message.ShouldBe(Error.Message[Error.InvalidLoginGuardianIdentifier]);

        var dto = await _accountAppService.VerifyCodeAsync(new VerifyCodeInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = Guid.NewGuid(),
            Code = Code,
            Salt = salt,
            GuardianIdentifierHash = hash
        });
        dto.Success.ShouldBe(false);
        dto.Message.ShouldBe(Error.Message[Error.InvalidLoginGuardianIdentifier]);


        var resultDto = await _accountAppService.VerifyCodeAsync(new VerifyCodeInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = id,
            Code = Code,
            Salt = salt,
            GuardianIdentifierHash = hash
        });
        resultDto.Success.ShouldBe(false);
        resultDto.Message.ShouldBe(Error.Message[Error.WrongCode]);
    }


    [Fact]
    public async Task VerifyCode_Register_Success_Test()
    {
        var salt = Guid.NewGuid().ToString().Replace("-", "");
        var hash = HashHelper.ComputeFrom(salt + HashHelper.ComputeFrom(DefaultEmailAddress).ToHex()).ToHex();
        var id = await SendVerificationRequest();
        var dto = await _accountAppService.VerifyCodeAsync(new VerifyCodeInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = Guid.NewGuid(),
            Code = Code,
            Salt = salt,
            GuardianIdentifierHash = hash
        });
        dto.Success.ShouldBe(false);
        dto.Message.ShouldBe(Error.Message[Error.InvalidLoginGuardianIdentifier]);
    }



    [Fact]
    public async Task VerifyCode_InputIsNullOrEmpty_Test()
    {
        var salt = Guid.NewGuid().ToString().Replace("-", "");
        var hash = HashHelper.ComputeFrom(salt + HashHelper.ComputeFrom(DefaultEmailAddress).ToHex()).ToHex();
        var id = await SendVerificationRequest();
        var dto = await _accountAppService.VerifyCodeAsync(new VerifyCodeInput
        {
            VerifierSessionId = id,
            Code = Code,
            Salt = salt,
            GuardianIdentifierHash = hash
        });
        dto.Success.ShouldBe(false);
        dto.Message.ShouldBe(Error.Message[Error.NullOrEmptyInput]);

        var resultDto = await _accountAppService.VerifyCodeAsync(new VerifyCodeInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            Code = Code,
            Salt = salt,
            GuardianIdentifierHash = hash
        });
        resultDto.Success.ShouldBe(false);
        resultDto.Message.ShouldBe(Error.Message[Error.NullOrEmptyInput]);

        var responseResultDto = await _accountAppService.VerifyCodeAsync(new VerifyCodeInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = id,
            Salt = salt,
            GuardianIdentifierHash = hash
        });
        responseResultDto.Success.ShouldBe(false);
        responseResultDto.Message.ShouldBe(Error.Message[Error.NullOrEmptyInput]);

        var result = await _accountAppService.VerifyCodeAsync(new VerifyCodeInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = id,
            Code = Code,
            GuardianIdentifierHash = hash
        });
        result.Success.ShouldBe(false);
        result.Message.ShouldBe(Error.Message[Error.NullOrEmptyInput]);

        var responseResult = await _accountAppService.VerifyCodeAsync(new VerifyCodeInput
        {
            GuardianIdentifier = DefaultEmailAddress,
            VerifierSessionId = id,
            Code = Code,
            Salt = salt
        });
        responseResult.Success.ShouldBe(false);
        responseResult.Message.ShouldBe(Error.Message[Error.NullOrEmptyInput]);
    }


    private async Task<Guid> SendVerificationRequest()
    {
        var verifierSessionId = Guid.NewGuid();
        var result = await _accountAppService.SendVerificationRequestAsync(new SendVerificationRequestInput
        {
            Type = Type,
            VerifierSessionId = verifierSessionId,
            GuardianIdentifier = DefaultEmailAddress,
        });
        return verifierSessionId;
    }
}