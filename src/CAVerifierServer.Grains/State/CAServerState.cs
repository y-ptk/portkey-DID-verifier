using CAVerifierServer.Account;

namespace CAVerifierServer.Grains.State;

public class CAServerState
{
    public DidServerList DidServerList { get; set; }

    public DateTime UpDateTime { get; set; }

}

