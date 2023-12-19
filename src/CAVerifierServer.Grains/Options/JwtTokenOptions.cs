namespace CAVerifierServer.Telegram.Options;

public class JwtTokenOptions
{
    public string PublicKey { get; set; } 
    public string PrivateKey { get; set; }
    public string Issuer { get; set; }
    public IEnumerable<string> Audiences { get; set; }
    public int expire { get; set; }
}