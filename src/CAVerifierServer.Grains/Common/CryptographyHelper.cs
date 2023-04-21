using AElf;
using AElf.Cryptography;
using AElf.Types;
using CAVerifierServer.Account;

namespace CAVerifierServer.Common;

public class CryptographyHelper
{
    public static GenerateSignatureOutput GenerateSignature(GuardianIdentifierType guardianType, string salt,
        string guardianIdentifierHash,
        string privateKey)
    {
        //create signature
        var verifierSPublicKey =
            CryptoHelper.FromPrivateKey(ByteArrayHelper.HexStringToByteArray(privateKey)).PublicKey;
        var verifierAddress = Address.FromPublicKey(verifierSPublicKey);
        var data =
            $"{(int)guardianType},{guardianIdentifierHash},{DateTime.UtcNow},{verifierAddress.ToBase58()},{salt}";
        var hashByteArray = HashHelper.ComputeFrom(data).ToByteArray();
        var signature =
            CryptoHelper.SignWithPrivateKey(ByteArrayHelper.HexStringToByteArray(privateKey), hashByteArray);
        return new GenerateSignatureOutput
        {
            Data = data,
            Signature = signature.ToHex()
        };
    }
}