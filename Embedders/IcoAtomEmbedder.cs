using PS1Stealth.Core;

namespace PS1Stealth.Embedders;

/// <summary>
/// Embeds payload using ICO header reserved bytes and padding
/// Similar to beheader's MP4 atom technique but for ICO files
/// </summary>
public class IcoAtomEmbedder : IEmbedder
{
    private const int MaxHeaderPayload = 200; // Similar to beheader's --extra limit

    public async Task<byte[]> EmbedAsync(byte[] carrierData, PayloadData payload)
    {
        // For ICO format, we can use:
        // 1. Reserved bytes in header
        // 2. Padding between directory entries
        // 3. Appended data after all images

        var payloadBytes = BinaryHelper.PreparePayload(
            payload.ScriptContent,
            payload.Password,
            payload.UseCompression);

        // Use append method for larger payloads
        // For small payloads, could use header space

        if (payloadBytes.Length <= MaxHeaderPayload)
        {
            return EmbedInHeader(carrierData, payloadBytes);
        }
        else
        {
            return EmbedAsAppend(carrierData, payloadBytes);
        }
    }

    public async Task<string> ExtractAsync(byte[] polyglotData, string? password = null)
    {
        // Try to extract from header first
        try
        {
            return ExtractFromHeader(polyglotData, password);
        }
        catch
        {
            // Fall back to appended data
            return ExtractFromAppend(polyglotData, password);
        }
    }

    private byte[] EmbedInHeader(byte[] carrierData, byte[] payload)
    {
        if (carrierData.Length < 22)
            throw new Exception("Invalid ICO file");

        // ICO Header: [0-1] Reserved, [2-3] Type, [4-5] Count
        // Directory Entry: 16 bytes per image
        
        var imageCount = BitConverter.ToUInt16(carrierData, 4);
        var headerSize = 6 + (imageCount * 16);

        // Find first image data offset
        var firstImageOffset = BitConverter.ToInt32(carrierData, 18); // Offset from first directory entry

        // Calculate available space
        var availableSpace = firstImageOffset - headerSize;
        
        if (payload.Length > availableSpace)
            throw new Exception($"Payload too large for header space: {payload.Length} > {availableSpace}");

        var output = new byte[carrierData.Length + payload.Length];
        
        // Copy header
        Buffer.BlockCopy(carrierData, 0, output, 0, headerSize);
        
        // Insert payload
        Buffer.BlockCopy(payload, 0, output, headerSize, payload.Length);
        
        // Copy rest of file
        Buffer.BlockCopy(carrierData, headerSize, output, headerSize + payload.Length, carrierData.Length - headerSize);

        // Update image offsets in directory entries
        for (int i = 0; i < imageCount; i++)
        {
            var entryOffset = 6 + (i * 16);
            var imageOffset = BitConverter.ToInt32(output, entryOffset + 12);
            var newOffset = imageOffset + payload.Length;
            Buffer.BlockCopy(BitConverter.GetBytes(newOffset), 0, output, entryOffset + 12, 4);
        }

        return output;
    }

    private byte[] EmbedAsAppend(byte[] carrierData, byte[] payload)
    {
        // Simply append payload to end
        var output = new byte[carrierData.Length + payload.Length];
        Buffer.BlockCopy(carrierData, 0, output, 0, carrierData.Length);
        Buffer.BlockCopy(payload, 0, output, carrierData.Length, payload.Length);
        return output;
    }

    private string ExtractFromHeader(byte[] data, string? password)
    {
        if (data.Length < 22)
            throw new Exception("Invalid ICO file");

        var imageCount = BitConverter.ToUInt16(data, 4);
        var headerSize = 6 + (imageCount * 16);
        
        // Get first image offset (should be adjusted if data was embedded)
        var firstImageOffset = BitConverter.ToInt32(data, 18);

        if (firstImageOffset <= headerSize)
            throw new Exception("No embedded data in header");

        var payloadSize = firstImageOffset - headerSize;
        var payloadData = new byte[payloadSize];
        Buffer.BlockCopy(data, headerSize, payloadData, 0, payloadSize);

        return BinaryHelper.ExtractPayload(payloadData, password);
    }

    private string ExtractFromAppend(byte[] data, string? password)
    {
        // Find where ICO data ends by parsing structure
        if (data.Length < 22)
            throw new Exception("Invalid ICO file");

        var imageCount = BitConverter.ToUInt16(data, 4);
        
        // Find the last image's end
        int maxEnd = 0;
        for (int i = 0; i < imageCount; i++)
        {
            var entryOffset = 6 + (i * 16);
            var imageSize = BitConverter.ToInt32(data, entryOffset + 8);
            var imageOffset = BitConverter.ToInt32(data, entryOffset + 12);
            var imageEnd = imageOffset + imageSize;
            maxEnd = Math.Max(maxEnd, imageEnd);
        }

        if (maxEnd >= data.Length)
            throw new Exception("No appended payload found");

        var payloadSize = data.Length - maxEnd;
        var payloadData = new byte[payloadSize];
        Buffer.BlockCopy(data, maxEnd, payloadData, 0, payloadSize);

        return BinaryHelper.ExtractPayload(payloadData, password);
    }
}
