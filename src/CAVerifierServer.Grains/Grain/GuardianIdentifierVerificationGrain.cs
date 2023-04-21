using System.Text;
using AElf;
using AElf.Cryptography;
using AElf.Types;
using CAVerifierServer.Account;
using CAVerifierServer.Application;
using CAVerifierServer.Grains.Dto;
using CAVerifierServer.Grains.Options;
using CAVerifierServer.Grains.State;
using Microsoft.Extensions.Options;
using NUglify.Helpers;
using Orleans.Providers;
using Orleans;

namespace CAVerifierServer.Grains.Grain;

[StorageProvider(ProviderName = "Default")]
public class GuardianIdentifierVerificationGrain : Grain<GuardianIdentifierVerificationState>,
    IGuardianIdentifierVerificationGrain
{
    private const string BASECODE = "0123456789";
    static Random ranNum = new Random((int)DateTime.Now.Ticks);

    private readonly VerifierCodeOptions _verifierCodeOptions;
    private readonly VerifierAccountOptions _verifierAccountOptions;

    public GuardianIdentifierVerificationGrain(IOptions<VerifierCodeOptions> verifierCodeOptions,
        IOptions<VerifierAccountOptions> verifierAccountOptions)
    {
        _verifierCodeOptions = verifierCodeOptions.Value;
        _verifierAccountOptions = verifierAccountOptions.Value;
    }

    private Task<string> GetCodeAsync(int length)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < length; i++)
        {
            var rnNum = ranNum.Next(BASECODE.Length);
            builder.Append(BASECODE[rnNum]);
        }

        return Task.FromResult(builder.ToString());
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }

    public async Task<GrainResultDto<VerifyCodeDto>> GetVerifyCodeAsync(SendVerificationRequestInput input)
    {
        //clean expireCode And Validate
        var grainDto = new GrainResultDto<VerifyCodeDto>();
        var verifications = State.GuardianTypeVerifications;
        if (verifications != null)
        {
            verifications.RemoveAll(p =>
                p.VerificationCodeSentTime.AddMinutes(_verifierCodeOptions.CodeExpireTime) < DateTime.UtcNow);
            verifications.RemoveAll(p => p.Verified);
            await WriteStateAsync();
            var totalList = verifications.Where(p =>
                    p.VerificationCodeSentTime.AddMinutes(_verifierCodeOptions.GetCodeFrequencyTimeLimit) >
                    DateTime.UtcNow)
                .ToList();
            if (totalList.Count >= _verifierCodeOptions.GetCodeFrequencyLimit)
            {
                grainDto.Message = "The interval between sending two verification codes is less than 60s";
                return grainDto;
            }

            if (verifications.Any(p => p.VerifierSessionId == input.VerifierSessionId))
            {
                grainDto.Success = true;
                grainDto.Data = new VerifyCodeDto
                {
                    VerifierCode = verifications.FirstOrDefault(p => p.VerifierSessionId == input.VerifierSessionId)
                        ?.VerificationCode
                };
                await WriteStateAsync();
                return grainDto;
            }
        }

        var guardianIdentifierVerification = new GuardianIdentifierVerification
        {
            GuardianIdentifier = input.GuardianIdentifier,
            GuardianType = input.Type,
            VerifierSessionId = input.VerifierSessionId
        };
        //create code
        var randomCode = await GetCodeAsync(6);
        guardianIdentifierVerification.VerificationCode = randomCode;
        guardianIdentifierVerification.VerificationCodeSentTime = DateTime.UtcNow;
        verifications ??= new List<GuardianIdentifierVerification>();
        verifications.Add(guardianIdentifierVerification);
        State.GuardianTypeVerifications = verifications;
        grainDto.Success = true;
        grainDto.Data = new VerifyCodeDto
        {
            VerifierCode = randomCode
        };
        await WriteStateAsync();
        return grainDto;
    }


    private int VerifyCodeAsync(GuardianIdentifierVerification guardianIdentifierVerification, string code)
    {
        //Already Verified
        if (guardianIdentifierVerification.Verified)
        {
            return Error.Verified;
        }

        //verify code exceed the time limit
        if (guardianIdentifierVerification.VerificationCodeSentTime.AddMinutes(_verifierCodeOptions.CodeExpireTime) <
            DateTime.UtcNow)
        {
            return Error.Timeout;
        }

        //error code times
        if (guardianIdentifierVerification.ErrorCodeTimes > _verifierCodeOptions.RetryTimes)
        {
            return Error.TooManyRetries;
        }

        //verify code is right
        if (code == guardianIdentifierVerification.VerificationCode)
        {
            return 0;
        }

        guardianIdentifierVerification.ErrorCodeTimes++;
        return Error.WrongCode;
    }

    //move to Options
    private Dictionary<string, GuardianType> GuardianTypeDic = new()
    {
        { "Email", GuardianType.OfEmail }
    };

    private GenerateSignatureOutput GenerateSignature(string guardianType, string salt, string guardianIdentifierHash,
        string privateKey)
    {
        //create signature
        var verifierSPublicKey =
            CryptoHelper.FromPrivateKey(ByteArrayHelper.HexStringToByteArray(privateKey)).PublicKey;
        var verifierAddress = Address.FromPublicKey(verifierSPublicKey);
        var data =
            $"{(int)GuardianTypeDic[guardianType]},{guardianIdentifierHash},{DateTime.UtcNow},{verifierAddress.ToBase58()},{salt}";
        var hashByteArray = HashHelper.ComputeFrom(data).ToByteArray();
        var signature =
            CryptoHelper.SignWithPrivateKey(ByteArrayHelper.HexStringToByteArray(privateKey), hashByteArray);
        return new GenerateSignatureOutput
        {
            Data = data,
            Signature = signature.ToHex()
        };
    }

    public async Task<GrainResultDto<UpdateVerifierSignatureDto>> VerifyAndCreateSignatureAsync(VerifyCodeInput input)
    {
        var dto = new GrainResultDto<UpdateVerifierSignatureDto>();
        var verifications = State.GuardianTypeVerifications;
        if (verifications == null)
        {
            dto.Message = Error.Message[Error.InvalidLoginGuardianIdentifier];
            return dto;
        }
        verifications = verifications.Where(p => p.VerifierSessionId == input.VerifierSessionId).ToList();
        if (verifications.Count == 0)
        {
            dto.Message = Error.Message[Error.InvalidVerifierSessionId];
            return dto;
        }

        var guardianTypeVerification = verifications[0];
        var errorCode = VerifyCodeAsync(guardianTypeVerification, input.Code);
        if (errorCode > 0)
        {
            dto.Message = Error.Message[errorCode];
            return dto;
        }

        guardianTypeVerification.VerifiedTime = DateTime.UtcNow;
        guardianTypeVerification.Verified = true;
        guardianTypeVerification.Salt = input.Salt;
        guardianTypeVerification.GuardianIdentifierHash = input.GuardianIdentifierHash;
        var signature = GenerateSignature(guardianTypeVerification.GuardianType, guardianTypeVerification.Salt,
            guardianTypeVerification.GuardianIdentifierHash, _verifierAccountOptions.PrivateKey);
        guardianTypeVerification.VerificationDoc = signature.Data;
        guardianTypeVerification.Signature = signature.Signature;
        dto.Success = true;
        dto.Data = new UpdateVerifierSignatureDto
        {
            Data = signature.Data,
            Signature = signature.Signature
        };
        await WriteStateAsync();
        return dto;
    }
}