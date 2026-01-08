using System.Drawing;
using System.Drawing.Imaging;
using PS1Stealth.Core;

namespace PS1Stealth.Embedders;

/// <summary>
/// Embeds payload using LSB (Least Significant Bit) steganography in PNG images
/// </summary>
public class ImageLSBEmbedder : IEmbedder
{
    public async Task<byte[]> EmbedAsync(byte[] carrierData, PayloadData payload)
    {
        // Load image from carrier data
        using var ms = new MemoryStream(carrierData);
        using var originalImage = new Bitmap(ms);

        // Prepare payload
        var payloadBytes = BinaryHelper.PreparePayload(
            payload.ScriptContent, 
            payload.Password, 
            payload.UseCompression);

        // Check if image has enough capacity
        var maxCapacity = (originalImage.Width * originalImage.Height * 3) / 8; // 3 channels, 1 bit per byte
        if (payloadBytes.Length > maxCapacity)
            throw new Exception($"Image too small. Need {payloadBytes.Length} bytes, have {maxCapacity} bytes capacity");

        // Create a copy to modify
        var bitmap = new Bitmap(originalImage.Width, originalImage.Height, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.DrawImage(originalImage, 0, 0);
        }

        // Embed payload
        EmbedDataInImage(bitmap, payloadBytes);

        // Save to memory stream
        var outputStream = new MemoryStream();
        bitmap.Save(outputStream, ImageFormat.Png);
        
        return outputStream.ToArray();
    }

    public async Task<string> ExtractAsync(byte[] polyglotData, string? password = null)
    {
        using var ms = new MemoryStream(polyglotData);
        using var bitmap = new Bitmap(ms);

        var extractedData = ExtractDataFromImage(bitmap);
        return BinaryHelper.ExtractPayload(extractedData, password);
    }

    private unsafe void EmbedDataInImage(Bitmap bitmap, byte[] data)
    {
        var bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadWrite,
            bitmap.PixelFormat);

        try
        {
            byte* ptr = (byte*)bitmapData.Scan0;
            int bytesPerPixel = 3; // RGB
            int stride = bitmapData.Stride;

            int dataIndex = 0;
            int bitIndex = 0;

            for (int y = 0; y < bitmap.Height && dataIndex < data.Length; y++)
            {
                byte* row = ptr + (y * stride);

                for (int x = 0; x < bitmap.Width && dataIndex < data.Length; x++)
                {
                    for (int c = 0; c < bytesPerPixel && dataIndex < data.Length; c++)
                    {
                        int pixelIndex = x * bytesPerPixel + c;
                        
                        // Get the bit to embed
                        byte bit = (byte)((data[dataIndex] >> (7 - bitIndex)) & 1);
                        
                        // Clear LSB and set new bit
                        row[pixelIndex] = (byte)((row[pixelIndex] & 0xFE) | bit);

                        bitIndex++;
                        if (bitIndex == 8)
                        {
                            bitIndex = 0;
                            dataIndex++;
                        }
                    }
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }
    }

    private unsafe byte[] ExtractDataFromImage(Bitmap bitmap)
    {
        var bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format24bppRgb);

        try
        {
            byte* ptr = (byte*)bitmapData.Scan0;
            int bytesPerPixel = 3;
            int stride = bitmapData.Stride;

            // Extract enough data to read the header first
            var headerData = new byte[12];
            ExtractBytes(ptr, stride, bitmap.Width, bitmap.Height, bytesPerPixel, headerData);

            // Parse header to get actual payload size
            var totalSize = BitConverter.ToInt32(headerData, 4);
            var fullData = new byte[12 + totalSize];
            
            // Extract full payload
            ExtractBytes(ptr, stride, bitmap.Width, bitmap.Height, bytesPerPixel, fullData);

            return fullData;
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }
    }

    private unsafe void ExtractBytes(byte* ptr, int stride, int width, int height, int bytesPerPixel, byte[] output)
    {
        int dataIndex = 0;
        int bitIndex = 0;
        byte currentByte = 0;

        for (int y = 0; y < height && dataIndex < output.Length; y++)
        {
            byte* row = ptr + (y * stride);

            for (int x = 0; x < width && dataIndex < output.Length; x++)
            {
                for (int c = 0; c < bytesPerPixel && dataIndex < output.Length; c++)
                {
                    int pixelIndex = x * bytesPerPixel + c;
                    
                    // Extract LSB
                    byte bit = (byte)(row[pixelIndex] & 1);
                    currentByte = (byte)((currentByte << 1) | bit);

                    bitIndex++;
                    if (bitIndex == 8)
                    {
                        output[dataIndex] = currentByte;
                        currentByte = 0;
                        bitIndex = 0;
                        dataIndex++;
                    }
                }
            }
        }
    }
}
