using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CAVerifierServer.Hubs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace CAVerifierServer.Hubs;

public interface IHubCacheProvider
{
    Task SetResponseAsync<T>(HubResponseCacheEntity<T> res, string clientId);
    Task<List<HubResponseCacheEntity<object>>> GetResponseByClientId(string clientId);
    Task<HubResponseCacheEntity<object>> GetRequestById(string requestId);

    Task RemoveResponseByClientId(string clientId, string requestId);
}

public class HubCacheProvider : IHubCacheProvider, ISingletonDependency
{
    private readonly IRedisCacheProvider _redisCacheProvider;
    private readonly HubCacheOptions _hubCacheOptions;
    private readonly ILogger<HubCacheProvider> _logger;

    public HubCacheProvider(IRedisCacheProvider redisCacheProvider, IOptions<HubCacheOptions> hubCacheOptions, ILogger<HubCacheProvider> logger)
    {
        _redisCacheProvider = redisCacheProvider;
        _logger = logger;
        _hubCacheOptions = hubCacheOptions.Value;
    }


    public async Task SetResponseAsync<T>(HubResponseCacheEntity<T> res, string clientId)
    {
        var resJsonStr = JsonSerializer.Serialize(res);
        var requestCacheKey = MakeResponseCacheKey(res.Response.RequestId);
        var clientCacheKey = MakeClientCacheKey(clientId);
        await _redisCacheProvider.Set(requestCacheKey, resJsonStr, GetMethodResponseTtl(res.Method));
        _redisCacheProvider.HSetWithExpire(clientCacheKey, res.Response.RequestId, "", GetClientCacheTtl());
        _logger.LogInformation($"set requestCacheKey={requestCacheKey}, clientCacheKey={clientCacheKey}, requestId={res.Response.RequestId}");
    }

    public async Task<List<HubResponseCacheEntity<object>>> GetResponseByClientId(string clientId)
    {
        var requestIds = await _redisCacheProvider.HGetAll(MakeClientCacheKey(clientId));
        var ans = new List<HubResponseCacheEntity<object>>();
        if (requestIds == null || requestIds.Length == 0)
        {
            return ans;
        }

        var responseKeys = requestIds.Select(requestId => MakeResponseCacheKey(requestId.Name)).ToList();
        var ansValues = await _redisCacheProvider.BatchGet(responseKeys);
        ans.AddRange(ansValues.Select(kv => JsonSerializer.Deserialize<HubResponseCacheEntity<object>>(kv.Value)));
        return ans;
    }

    public async Task<HubResponseCacheEntity<object>> GetRequestById(string requestId)
    {
        string jsonStr = await _redisCacheProvider.Get(MakeResponseCacheKey(requestId));
        return jsonStr == null ? null : JsonSerializer.Deserialize<HubResponseCacheEntity<object>>(jsonStr);
    }

    public async Task RemoveResponseByClientId(string clientId, string requestId)
    {
        var requestCacheKey = MakeResponseCacheKey(requestId);
        var clientCacheKey = MakeClientCacheKey(clientId);
        _redisCacheProvider.HashDelete(clientCacheKey, requestId);
        _redisCacheProvider.Delete(requestCacheKey);
    }

    private string MakeResponseCacheKey(string requestId)
    {
        return $"hub_req_cache:{requestId}";
    }

    private string MakeClientCacheKey(string clientId)
    {
        return $"hub_cli_cache:{clientId}";
    }

    private TimeSpan GetMethodResponseTtl(string method)
    {
        if (_hubCacheOptions != null && _hubCacheOptions.MethodResponseTtl.TryGetValue(method, out var value))
        {
            return new TimeSpan(0, 0, value);
        }

        return new TimeSpan(0, 0, _hubCacheOptions.DefaultResponseTtl);
    }

    private TimeSpan GetClientCacheTtl()
    {
        var max = _hubCacheOptions.DefaultResponseTtl;
        if (_hubCacheOptions.MethodResponseTtl == null)
        {
            return new TimeSpan(0, 0, max);
        }

        max = _hubCacheOptions.MethodResponseTtl.Select(kv => kv.Value).Prepend(max).Max();

        return new TimeSpan(0, 0, max);
    }
}

public class HubResponseCacheEntity<T>
{
    public HubResponseCacheEntity(T body, string requestId, string method)
    {
        Response = new HubResponse<T>() { RequestId = requestId, Body = body };
        Method = method;
    }

    public HubResponse<T> Response { get; set; }
    public string Method { get; set; }

    public class HubResponse<T>
    {
        public string RequestId { get; set; }
        public T Body { get; set; }
    }
}