namespace CAVerifierServer.Grains.Dto;

public class JwkDto
{
    public string Kty { get; set; }
    public string Alg { get; set; }
    public string Use { get; set; }
    public string Kid { get; set; }
    public string N { get; set; }
    public string E { get; set; }
}