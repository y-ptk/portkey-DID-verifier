using System.Threading.Tasks;
using CAVerifierServer.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace CAVerifierServer.Hubs;

public interface ICAHubProvider
{
    public Task ResponseAsync<T>(HubResponse<T> res, string clientId, string method, bool isFirstTime = true);
}

public class CAHubProvider : ICAHubProvider, ISingletonDependency
{
    private readonly IConnectionProvider _connectionProvider;
    private readonly IHubContext<CAHub> _hubContext;
    private readonly IHubCacheProvider _hubCacheProvider;
    private readonly ILogger<CAHubProvider> _logger;

    public CAHubProvider(IConnectionProvider connectionProvider, IHubContext<CAHub> hubContext, ILogger<CAHubProvider> logger, IHubCacheProvider hubCacheProvider)
    {
        _connectionProvider = connectionProvider;
        _hubContext = hubContext;
        _logger = logger;
        _hubCacheProvider = hubCacheProvider;
    }

    public async Task ResponseAsync<T>(HubResponse<T> res, string clientId, string method, bool isFirstTime = true)
    {
        if (isFirstTime)
        {
            _hubCacheProvider.SetResponseAsync(new HubResponseCacheEntity<T>(res.Body, res.RequestId, method), clientId);
        }

        var connection = _connectionProvider.GetConnectionByClientId(clientId);
        if (connection == null)
        {
            _logger.LogError($"connection not found by clientId={clientId}");
            return;
        }

        _logger.LogInformation($"provider sync requestId={res.RequestId} to clientId={clientId} method={method}");
        await _hubContext.Clients.Clients(connection.ConnectionId).SendAsync(method, res);
    }
}