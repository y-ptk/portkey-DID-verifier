using System.Collections.Generic;

namespace CAVerifierServer.Hubs;

public class HubCacheOptions
{
    public Dictionary<string, int> MethodResponseTtl { get; set; }
    public int DefaultResponseTtl { get; set; }
}