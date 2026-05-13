using Core.Helpers;

namespace Core.Crypto;

public enum KEYGEN_ALGORITHMS
{
    RSA_PUBLIC,
    RSA_PRIVATE,
    ECDSA_PUBLIC,
    ECDSA_PRIVATE,
    AES_GCM
}

public class KeyStore
{
    private static string dumppath;

    public static IDictionary<string, byte[]> keystore = new Dictionary<string, byte[]>();

    public static void SetDumpPath(string dumpPath)
    {
        dumppath = dumpPath;
    }

    public static void RegisterKey(string name, KEYGEN_ALGORITHMS algo, byte[] key)
    {
        var algotag = algo switch
        {
            KEYGEN_ALGORITHMS.RSA_PUBLIC => "rsa-public",
            KEYGEN_ALGORITHMS.RSA_PRIVATE => "rsa-private",
            KEYGEN_ALGORITHMS.ECDSA_PUBLIC => "ecdsa-pubic",
            KEYGEN_ALGORITHMS.ECDSA_PRIVATE => "ecdsa-private",
            KEYGEN_ALGORITHMS.AES_GCM => "aes_gcm"
        };

        string title = $"{name}-{algo}";
        keystore[title] = key;
    }

    public static void Clear()
    {
        keystore.Clear();
    }

    public static void Backup()
    {
        if (!File.Exists(dumppath))
            Directory.CreateDirectory(dumppath);

        foreach (var entry in keystore)
        {
            var title = $"{entry.Key}_{DateTime.UtcNow}";
            var url = Path.Join(dumppath, title);
            File.WriteAllBytes(url, entry.Value);
        }
    }

    public static void Upload()
    {
        if (!Directory.Exists(dumppath))
            throw ServiceHelper.BumpError("Dumpath does exists.", "keystore upload", dumppath);

        foreach (var file in Directory.GetFiles(dumppath))
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(file);

                // Expected format: keyName_timestamp
                var parts = fileName.Split('_', 2);

                if (parts.Length < 1)
                    continue;

                var keyName = parts[0];
                var algo = keyName.Split('-')[1];

                byte[] data = File.ReadAllBytes(file);

                bool valid = algo switch
                {
                    "aes-gcm" => CryptoHelper.IsValidAesKey(data),
                    "ecdsa-public" => CryptoHelper.IsValidEcdsaPublicKey(data),
                    "ecdsa-private" => CryptoHelper.IsValidEcdsaPrivateKey(data),
                    "rsa-public" => CryptoHelper.IsValidRsaPublicKey(data),
                    "rsa-private" => CryptoHelper.IsValidRsaPublicKey(data)
                };

                if (!valid)
                    throw ServiceHelper.BumpError("Invalid crypto key.", "keystore upload", dumppath, file, data);

                // Restore into keystore
                if (keystore.ContainsKey(keyName))
                    keystore[keyName] = data;
                else
                    keystore.Add(keyName, data);
            }
            catch (Exception ex)
            {
                throw ServiceHelper.BumpError("Failed to load key from.", "keystore upload", dumppath, file, ex);
            }
        }
    }
}