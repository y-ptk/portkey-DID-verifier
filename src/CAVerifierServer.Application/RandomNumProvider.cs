using System;
using System.Text;

namespace CAVerifierServer;

public class RandomNumProvider
{
    private const string BASECODE = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    static Random ranNum = new Random((int)DateTime.Now.Ticks);

    public static string GetCode(int length)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < length; i++)
        {
            var rnNum = ranNum.Next(BASECODE.Length);
            builder.Append(BASECODE[rnNum]);
        }

        return builder.ToString();
    }
}