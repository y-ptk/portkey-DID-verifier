using System;
using System.Text.Json.Serialization;

namespace CAVerifierServer.Account;

public class SendVerificationRequestInput
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
    
    [JsonPropertyName("guardianIdentifier")]
    public string GuardianIdentifier { get; set; }

    [JsonPropertyName("VerifierSessionId")]
    public Guid VerifierSessionId{ get; set; }

}