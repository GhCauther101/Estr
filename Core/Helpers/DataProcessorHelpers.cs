using System.Text;

namespace Core.Helpers;

public static class DataProcessorHelpers
{
    public static (int, IList<byte[]>) SplitIncomeStream(byte[] data, byte[] delimeter, byte[] startTag, byte[] endTag)
    {
        int start = 0;

        int frameState = 0;
        var cache = new List<byte[]>();

        for (int i = 0; i <= data.Length - delimeter.Length; i++)
        {
            if (data.AsSpan(i, startTag.Length).SequenceEqual(startTag))
            {
                frameState = 1;
                start = i + startTag.Length;
            }
            else frameState = 0;

            if (data.AsSpan(i, endTag.Length).SequenceEqual(endTag))
            {
                frameState = -1;
                start = data.Length;
                break;
            }

            if (!data.AsSpan(i, delimeter.Length).SequenceEqual(delimeter)) continue;

            var chunk = data.AsSpan(start, i - start);
            if (chunk.Length > 0)
                cache.Add(chunk.ToArray());

            start = i + delimeter.Length;
            i += delimeter.Length - 1;
        }

        if (start < data.Length)
            cache.Add(data.AsMemory(start).ToArray());

        return (frameState, cache);
    }

    public static IDictionary<string, string> DecomposeHeaderset(byte[] rawData, byte[] priv)
    {
        byte[] decryptedRaws = CryptoHelper.RsaDecrypt(rawData, priv);
        string decodedJson = Encoding.UTF8.GetString(decryptedRaws);
        return JsonHelper.DecodeToStringDictionary(decodedJson);
    }
}
