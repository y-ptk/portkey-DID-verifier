using System.Threading.Tasks;
using CAVerifierServer.Account;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAVerifierServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("CAVerification")]
[Route("api/app/account")]
public class CAVerificationController : CAVerifierServerController
{
    private readonly IAccountAppService _accountAppService;

    public CAVerificationController(IAccountAppService accountAppService)
    {
        _accountAppService = accountAppService;
    }

    [HttpPost]
    [Route("sendVerificationRequest")]
    public async Task<ResponseResultDto<SendVerificationRequestDto>> SendVerificationRequestAsync(SendVerificationRequestInput input)
    {
        return await _accountAppService.SendVerificationRequestAsync(input);
    }


    [HttpPost]
    [Route("verifyCode")]
    public async Task<ResponseResultDto<VerifierCodeDto>> VerifyCodeAsync(VerifyCodeInput input)
    {
        return await _accountAppService.VerifyCodeAsync(input);
    }
}