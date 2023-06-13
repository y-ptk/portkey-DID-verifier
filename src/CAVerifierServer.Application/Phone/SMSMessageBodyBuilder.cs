namespace CAVerifierServer.Phone;

public class SMSMessageBodyBuilder
{
    public static string BuildBodyTemplate(string verifierName, string code)
    {
        return "[" + verifierName + "] PORTKEY Verification Code: " + code + ". " +
               "This verification code will expire in 10 minutes. If you did not request this message, please ignore it.";
    }
}