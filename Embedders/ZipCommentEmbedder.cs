using PS1Stealth.Core;

namespace PS1Stealth.Embedders;

/// <summary>
/// Embeds payload in ZIP comment field
/// Works with ZIP, JAR, APK, DOCX, XLSX, and other ZIP-based formats
/// </summary>
public class ZipCommentEmbedder : IEmbedder
{
    private static readonly byte[] EndOfCentralDirSignature = { 0x50, 0x4B, 0x05, 0x06 };

    public async Task<byte[]> EmbedAsync(byte[] carrierData, PayloadData payload)
    {
        // Prepare payload
        var payloadBytes = BinaryHelper.PreparePayload(
            payload.ScriptContent,
            payload.Password,
            payload.UseCompression);

        // Find End of Central Directory record
        var eocdIndex = FindEndOfCentralDirectory(carrierData);
        if (eocdIndex == -1)
            throw new Exception("Invalid ZIP file - no End of Central Directory found");

        // ZIP comment has max size of 65535 bytes
        if (payloadBytes.Length > 65535)
            throw new Exception($"Payload too large for ZIP comment: {payloadBytes.Length} bytes (max 65535)");

        // EOCD structure:
        // [0-3]  Signature (0x06054b50)
        // [4-5]  Disk number
        // [6-7]  Disk with central directory
        // [8-9]  Entries on this disk
        // [10-11] Total entries
        // [12-15] Central directory size
        // [16-19] Central directory offset
        // [20-21] Comment length
        // [22+]   Comment

        var output = new MemoryStream();
        
        // Write everything up to comment length field
        output.Write(carrierData, 0, eocdIndex + 20);
        
        // Write new comment length
        output.Write(BitConverter.GetBytes((ushort)payloadBytes.Length), 0, 2);
        
        // Write payload as comment
        output.Write(payloadBytes, 0, payloadBytes.Length);

        return output.ToArray();
    }

    public async Task<string> ExtractAsync(byte[] polyglotData, string? password = null)
    {
        // Find End of Central Directory
        var eocdIndex = FindEndOfCentralDirectory(polyglotData);
        if (eocdIndex == -1)
            throw new Exception("Invalid ZIP file");

        // Read comment length
        var commentLength = BitConverter.ToUInt16(polyglotData, eocdIndex + 20);
        
        if (commentLength == 0)
            throw new Exception("No embedded payload found");

        // Extract comment data
        var commentStart = eocdIndex + 22;
        if (commentStart + commentLength > polyglotData.Length)
            throw new Exception("Invalid ZIP structure");

        var payloadData = new byte[commentLength];
        Buffer.BlockCopy(polyglotData, commentStart, payloadData, 0, commentLength);

        return BinaryHelper.ExtractPayload(payloadData, password);
    }

    private int FindEndOfCentralDirectory(byte[] data)
    {
        // Search from the end (EOCD should be at the end of the file)
        for (int i = data.Length - 22; i >= 0; i--) // 22 = minimum EOCD size
        {
            if (data[i] == EndOfCentralDirSignature[0] &&
                data[i + 1] == EndOfCentralDirSignature[1] &&
                data[i + 2] == EndOfCentralDirSignature[2] &&
                data[i + 3] == EndOfCentralDirSignature[3])
            {
                return i;
            }
        }
        return -1;
    }
}
