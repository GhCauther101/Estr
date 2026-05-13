using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Core.Helpers;

public class DataProcessorHelpers
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

public static class CommonUtils
{
    public static readonly Func<string, bool> IsEmptyString = (value) => string.IsNullOrEmpty(value) && string.IsNullOrWhiteSpace(value);
    public static readonly Func<int, bool> IsActiveStream = (state) => (state switch { 0 => true, 1 => true, -1 => false });
    public static readonly Func<IDictionary<string, string>, bool> IsEmptySet = (map) => map == null && map.Count == 0;
}

public static partial class CryptoHelper
{
    // ================================
    // AES-GCM (256-bit)
    // ================================

    public static byte[] GenerateAesKey(int size = 32) // 32 bytes = 256-bit
    {
        byte[] key = new byte[size];
        RandomNumberGenerator.Fill(key);
        return key;
    }

    public static (byte[] cipher, byte[] nonce, byte[] tag) AesGcmEncrypt(byte[] key, byte[] plaintext)
    {
        byte[] nonce = new byte[12]; // recommended size
        RandomNumberGenerator.Fill(nonce);

        byte[] cipher = new byte[plaintext.Length];
        byte[] tag = new byte[16];

        using var aes = new AesGcm(key);

        aes.Encrypt(nonce, plaintext, cipher, tag);

        return (cipher, nonce, tag);
    }

    public static byte[] AesGcmDecrypt(byte[] key, byte[] cipher, byte[] nonce, byte[] tag)
    {
        byte[] plaintext = new byte[cipher.Length];

        using var aes = new AesGcm(key);

        aes.Decrypt(nonce, cipher, tag, plaintext);

        return plaintext;
    }

    // ================================
    // ECDSA (Sign / Verify)
    // ================================

    public static (byte[] privateKey, byte[] publicKey) GenerateEcdsaKeys()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        byte[] privateKey = ecdsa.ExportECPrivateKey();
        byte[] publicKey = ecdsa.ExportSubjectPublicKeyInfo();

        return (privateKey, publicKey);
    }

    public static byte[] Sign(byte[] data, byte[] privateKey)
    {
        using var ecdsa = ECDsa.Create();

        ecdsa.ImportECPrivateKey(privateKey, out _);

        return ecdsa.SignData(data, HashAlgorithmName.SHA256);
    }

    public static bool Verify(byte[] data, byte[] signature, byte[] publicKey)
    {
        using var ecdsa = ECDsa.Create();

        ecdsa.ImportSubjectPublicKeyInfo(publicKey, out _);

        return ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256);
    }

    // ================================
    // RSA (KeyGen / Encrypt / Decrypt)
    // ================================

    public static (byte[] privateKey, byte[] publicKey) GenerateRsaKeys(int keySize = 4096)
    {
        using var rsa = RSA.Create(keySize);

        byte[] privateKey = rsa.ExportRSAPrivateKey();
        byte[] publicKey = rsa.ExportRSAPublicKey();

        return (privateKey, publicKey);
    }

    public static byte[] RsaEncrypt(byte[] data, byte[] publicKey)
    {
        using var rsa = RSA.Create();

        rsa.ImportRSAPublicKey(publicKey, out _);

        return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
    }

    public static byte[] RsaDecrypt(byte[] cipher, byte[] privateKey)
    {
        using var rsa = RSA.Create();

        rsa.ImportRSAPrivateKey(privateKey, out _);

        return rsa.Decrypt(cipher, RSAEncryptionPadding.OaepSHA256);
    }

    // ================================
    // KEY VALIDATION
    // ================================

    public static bool IsValidAesKey(byte[] key)
    {
        if (key == null) return false;

        // AES supports: 16, 24, 32 bytes
        return key.Length is 16 or 24 or 32;
    }

    public static bool IsValidRsaKeyPair(byte[] publicKey, byte[] privateKey)
    {
        try
        {
            using var rsaPub = RSA.Create();
            rsaPub.ImportRSAPublicKey(publicKey, out _);

            using var rsaPriv = RSA.Create();
            rsaPriv.ImportRSAPrivateKey(privateKey, out _);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsValidRsaPublicKey(byte[] publicKey)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportRSAPublicKey(publicKey, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsValidRsaPrivateKey(byte[] privateKey)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(privateKey, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsValidEcdsaKeyPair(byte[] publicKey, byte[] privateKey)
    {
        try
        {
            using var ecdsa = ECDsa.Create();

            ecdsa.ImportECPrivateKey(privateKey, out _);
            ecdsa.ImportSubjectPublicKeyInfo(publicKey, out _);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsValidEcdsaPrivateKey(byte[] privateKey)
    {
        try
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportECPrivateKey(privateKey, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsValidEcdsaPublicKey(byte[] publicKey)
    {
        try
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(publicKey, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public static class JsonHelper
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        WriteIndented = false, // compact by default
        PropertyNameCaseInsensitive = true
    };

    // ================================
    // ENCODE (object → JSON string)
    // ================================
    public static string Encode<T>(T obj, bool pretty = false)
    {
        var options = pretty
            ? new JsonSerializerOptions { WriteIndented = true }
            : Options;

        return JsonSerializer.Serialize(obj, options);
    }

    // ================================
    // DECODE (JSON → Dictionary<string, string>)
    // ================================
    public static IDictionary<string, string> DecodeToStringDictionary(string json)
    {
        var result = new Dictionary<string, string>();

        using var doc = JsonDocument.Parse(json);

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            result[prop.Name] = ConvertJsonElementToString(prop.Value);
        }

        return result;
    }

    // ================================
    // Helper: Convert JsonElement → string
    // ================================
    private static string ConvertJsonElementToString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString()!,
            JsonValueKind.Number => element.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null!,
            _ => element.ToString()
        };
    }

    // ================================
    // DECODE (JSON string → Dictionary<string, object>)
    // ================================
    public static IDictionary<string, object?> DecodeToObjectDictionary(string json)
    {
        var result = new Dictionary<string, object?>();

        using var doc = JsonDocument.Parse(json);

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            result[prop.Name] = ConvertJsonElementToObject(prop.Value);
        }

        return result;
    }

    private static object? ConvertJsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }
}

public static class FileHelper
{
    // ================================
    // TEXT FILES
    // ================================

    public static void WriteText(string path, string content, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

        File.WriteAllText(path, content, encoding);
    }

    public static string ReadText(string path, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

        return File.ReadAllText(path, encoding);
    }

    // Async versions
    public static async Task WriteTextAsync(string path, string content, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

        await File.WriteAllTextAsync(path, content, encoding);
    }

    public static async Task<string> ReadTextAsync(string path, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

        return await File.ReadAllTextAsync(path, encoding);
    }

    // ================================
    // BYTE FILES
    // ================================

    public static void WriteBytes(string path, byte[] data)
    {
        File.WriteAllBytes(path, data);
    }

    public static byte[] ReadBytes(string path)
    {
        return File.ReadAllBytes(path);
    }

    // Async versions
    public static async Task WriteBytesAsync(string path, byte[] data)
    {
        await File.WriteAllBytesAsync(path, data);
    }

    public static async Task<byte[]> ReadBytesAsync(string path)
    {
        return await File.ReadAllBytesAsync(path);
    }

    // ================================
    // STREAM (for large files)
    // ================================

    public static async Task CopyToFileAsync(Stream input, string path)
    {
        using var output = File.Create(path);
        await input.CopyToAsync(output);
    }

    public static async Task CopyFromFileAsync(string path, Stream output)
    {
        using var input = File.OpenRead(path);
        await input.CopyToAsync(output);
    }
}

public static class ServiceHelper 
{
    public static Exception BumpError(string message, string environmentLocation = "", object detail1 = null, object detail2 = null, object detail3 = null)
    {
        var banner = ComposeLocationBanner(environmentLocation);
        var line = $"{banner} {message}";
       
        var exception = new Exception(line);
        exception.Data["detail1"] = detail1;
        exception.Data["detail2"] = detail2;
        exception.Data["detail3"] = detail3;
        return exception;
    }

    public static string ComposeLocationBanner(string environmentLocation)
    {
        return $"{environmentLocation} >";
    }
}