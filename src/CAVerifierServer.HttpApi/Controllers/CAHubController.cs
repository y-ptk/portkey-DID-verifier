using System.Threading.Tasks;
using CAVerifierServer.Hubs;
using Microsoft.AspNetCore.Mvc;

namespace CAVerifierServer.Controllers;

[Area("app")]
[ControllerName("CAHub")]
[Route("api/app/account/hub")]
public class CAHubController : CAVerifierServerController
{
    private readonly IHubService _hubService;

    public CAHubController(IHubService hubService)
    {
        _hubService = hubService;
    }

    [HttpPost]
    [Route("ping")]
    public async Task<string> Ping(HubPingRequest request)
    {
        _hubService.Ping(request.Context, request.Content);
        return "OK";
    }

    [HttpPost]
    [Route("getResponse")]
    public async Task<HubResponse<object>> GetResponse(GetHubRequest request)
    {
        return await _hubService.GetResponse(request.Context);
    }
}