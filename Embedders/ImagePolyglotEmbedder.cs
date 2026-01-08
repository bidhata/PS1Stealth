using PS1Stealth.Core;
using System.Text;

namespace PS1Stealth.Embedders;

/// <summary>
/// Creates ICO polyglot files inspired by beheader's technique
/// The file works as both an ICO image and contains hidden data
/// </summary>
public class ImagePolyglotEmbedder : IEmbedder
{
    private static readonly byte[] IcoHeader = new byte[] { 0x00, 0x00, 0x01, 0x00 };

    public async Task<byte[]> EmbedAsync(byte[] carrierData, PayloadData payload)
    {
        // Prepare payload
        var payloadBytes = BinaryHelper.PreparePayload(
            payload.ScriptContent,
            payload.Password,
            payload.UseCompression);

        // Create polyglot structure
        // [ICO Header][Image Data][Hidden Payload Footer]
        
        var output = new MemoryStream();
        var writer = new BinaryWriter(output);

        // Write ICO header
        writer.Write((ushort)0); // Reserved
        writer.Write((ushort)1); // Type: ICO
        writer.Write((ushort)1); // Image count

        // ICO Directory Entry (16 bytes)
        int imageOffset = 6 + 16; // Header + directory entry
        
        // Try to detect image dimensions (simple PNG check)
        int width = 256, height = 256, bpp = 32;
        if (carrierData.Length > 24 && carrierData[0] == 0x89 && carrierData[1] == 'P')
        {
            // PNG - read dimensions from IHDR
            width = BinaryHelper.FromBigEndian32(carrierData, 16);
            height = BinaryHelper.FromBigEndian32(carrierData, 20);
            if (width > 256 || height > 256)
            {
                width = height = 0; // ICO uses 0 for 256x256
            }
        }

        writer.Write((byte)width);      // Width (0 = 256)
        writer.Write((byte)height);     // Height (0 = 256)
        writer.Write((byte)0);          // Color palette
        writer.Write((byte)0);          // Reserved
        writer.Write((ushort)1);        // Color planes
        writer.Write((ushort)bpp);      // Bits per pixel
        writer.Write((int)carrierData.Length); // Image data size
        writer.Write((int)imageOffset); // Image data offset

        // Write image data
        writer.Write(carrierData);

        // Write hidden payload at the end
        // This area is ignored by ICO parsers
        writer.Write(payloadBytes);

        return output.ToArray();
    }

    public async Task<string> ExtractAsync(byte[] polyglotData, string? password = null)
    {
        // The payload is appended after the ICO image data
        // We need to parse ICO structure to find where image ends

        if (polyglotData.Length < 22)
            throw new Exception("Invalid ICO file");

        // Read ICO directory entry to find image size and offset
        int imageSize = BitConverter.ToInt32(polyglotData, 14);
        int imageOffset = BitConverter.ToInt32(polyglotData, 18);

        // Payload starts after image data
        int payloadStart = imageOffset + imageSize;

        if (payloadStart >= polyglotData.Length)
            throw new Exception("No embedded payload found");

        // Extract payload portion
        var payloadData = new byte[polyglotData.Length - payloadStart];
        Buffer.BlockCopy(polyglotData, payloadStart, payloadData, 0, payloadData.Length);

        return BinaryHelper.ExtractPayload(payloadData, password);
    }
}
