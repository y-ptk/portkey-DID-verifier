using System;
using System.Threading.Tasks;
using CAVerifierServer.Account;
using Microsoft.Extensions.Logging;

namespace CAVerifierServer.Hubs;

public class HubService : CAVerifierServerAppService, IHubService
{
    private readonly ICAHubProvider _caHubProvider;
    private readonly IHubCacheProvider _hubCacheProvider;
    private readonly IConnectionProvider _connectionProvider;
    private readonly ILogger<HubService> _logger;

    public HubService(ICAHubProvider caHubProvider, IHubCacheProvider hubCacheProvider, IConnectionProvider connectionProvider, ILogger<HubService> logger)
    {
        _caHubProvider = caHubProvider;
        _hubCacheProvider = hubCacheProvider;
        _connectionProvider = connectionProvider;
        _logger = logger;
    }

    public async Task Ping(HubRequestContext context, string content)
    {
        _caHubProvider.ResponseAsync(new HubResponse<object>() { RequestId = context.RequestId, Body = new GenerateSignatureOutput { Signature = content } }, context.ClientId, method: "Sin");
    }

    public async Task<HubResponse<object>> GetResponse(HubRequestContext context)
    {
        var cacheRes = await _hubCacheProvider.GetRequestById(context.RequestId);
        if (cacheRes == null)
        {
            return null;
        }

        _hubCacheProvider.RemoveResponseByClientId(context.ClientId, context.RequestId);
        return new HubResponse<object>()
        {
            RequestId = cacheRes.Response.RequestId, Body = cacheRes.Response.Body
        };
    }

    public async Task RegisterClient(string clientId, string connectionId)
    {
        _connectionProvider.Add(clientId, connectionId);
    }

    public string UnRegisterClient(string connectionId)
    {
        return _connectionProvider.Remove(connectionId);
    }

    public async Task SendAllUnreadRes(string clientId)
    {
        var unreadRes = await _hubCacheProvider.GetResponseByClientId(clientId);
        if (unreadRes == null || unreadRes.Count == 0)
        {
            _logger.LogInformation($"clientId={clientId}'s unread res is null");
            return;
        }

        foreach (var res in unreadRes)
        {
            try
            {
                await _caHubProvider.ResponseAsync(new HubResponse<object>() { RequestId = res.Response.RequestId, Body = res.Response.Body }, clientId, res.Method, false);
                _logger.LogInformation($"syncOnConnect requestId={res.Response.RequestId} to clientId={clientId} method={res.Method}");
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"syncOnConnect failed requestId={res.Response.RequestId} to clientId={clientId} method={res.Method}, exception={e.Message}");
            }
        }
    }

    public async Task Ack(string clientId, string requestId)
    {
        await _hubCacheProvider.RemoveResponseByClientId(clientId, requestId);
    }
}