using System.Security.Cryptography;

namespace Core.Helpers;

public static class CryptoHelper
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