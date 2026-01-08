using System.Security.Cryptography;
using System.Text;

namespace PS1Stealth.Core;

public static class CryptoHelper
{
    private const int KeySize = 256;
    private const int Iterations = 100000;

    public static byte[] Encrypt(string plainText, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(32);
        var key = DeriveKey(password, salt);
        var iv = RandomNumberGenerator.GetBytes(16);

        using var aes = Aes.Create();
        aes.KeySize = KeySize;
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Format: [salt(32)][iv(16)][encrypted data]
        var result = new byte[salt.Length + iv.Length + encrypted.Length];
        Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
        Buffer.BlockCopy(iv, 0, result, salt.Length, iv.Length);
        Buffer.BlockCopy(encrypted, 0, result, salt.Length + iv.Length, encrypted.Length);

        return result;
    }

    public static string Decrypt(byte[] cipherData, string password)
    {
        if (cipherData.Length < 48) // Minimum: 32 (salt) + 16 (IV)
            throw new ArgumentException("Invalid cipher data");

        var salt = new byte[32];
        var iv = new byte[16];
        var encrypted = new byte[cipherData.Length - 48];

        Buffer.BlockCopy(cipherData, 0, salt, 0, 32);
        Buffer.BlockCopy(cipherData, 32, iv, 0, 16);
        Buffer.BlockCopy(cipherData, 48, encrypted, 0, encrypted.Length);

        var key = DeriveKey(password, salt);

        using var aes = Aes.Create();
        aes.KeySize = KeySize;
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var decrypted = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);

        return Encoding.UTF8.GetString(decrypted);
    }

    private static byte[] DeriveKey(string password, byte[] salt)
    {
        using var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        return deriveBytes.GetBytes(32); // 256 bits
    }

    public static byte[] Compress(byte[] data)
    {
        using var outputStream = new MemoryStream();
        using (var gzipStream = new System.IO.Compression.GZipStream(outputStream, System.IO.Compression.CompressionLevel.Optimal))
        {
            gzipStream.Write(data, 0, data.Length);
        }
        return outputStream.ToArray();
    }

    public static byte[] Decompress(byte[] data)
    {
        using var inputStream = new MemoryStream(data);
        using var gzipStream = new System.IO.Compression.GZipStream(inputStream, System.IO.Compression.CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        gzipStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }
}
