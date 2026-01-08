using PS1Stealth.Core;
using System.Text;

namespace PS1Stealth.Embedders;

/// <summary>
/// Embeds payload in PDF stream objects - inspired by beheader's PDF technique
/// </summary>
public class PdfPolyglotEmbedder : IEmbedder
{
    public async Task<byte[]> EmbedAsync(byte[] carrierData, PayloadData payload)
    {
        // Prepare payload
        var payloadBytes = BinaryHelper.PreparePayload(
            payload.ScriptContent,
            payload.Password,
            payload.UseCompression);

        // Find the end of PDF (%%EOF)
        var eofIndex = FindPdfEnd(carrierData);
        if (eofIndex == -1)
            throw new Exception("Invalid PDF file - no %%EOF marker found");

        // Create a hidden stream object
        var objectNumber = GetNextObjectNumber(carrierData);
        var streamObject = CreateStreamObject(objectNumber, payloadBytes);

        // Build output: [Original PDF][Hidden Stream Object][New %%EOF]
        var output = new MemoryStream();
        
        // Write original PDF up to %%EOF
        output.Write(carrierData, 0, eofIndex);
        
        // Write hidden object
        output.Write(Encoding.ASCII.GetBytes("\n"));
        output.Write(streamObject);
        
        // Write new %%EOF
        output.Write(Encoding.ASCII.GetBytes("\n%%EOF\n"));

        return output.ToArray();
    }

    public async Task<string> ExtractAsync(byte[] polyglotData, string? password = null)
    {
        // Find %%EOF
        var lastEof = FindLastIndex(polyglotData, "%%EOF");
        if (lastEof == -1) throw new Exception("Invalid PDF file");

        // Find endstream
        var endStreamPos = FindLastIndex(polyglotData, "endstream", lastEof);
        if (endStreamPos == -1) throw new Exception("No embedded payload found");

        // Find stream start - search for "stream"
        var streamPos = FindLastIndex(polyglotData, "stream", endStreamPos);
        if (streamPos == -1) throw new Exception("Malformed embedded payload");

        // Calculate data start handling newline (\n or \r\n)
        var dataStart = streamPos + 6; // "stream".Length
        if (dataStart < polyglotData.Length)
        {
            if (polyglotData[dataStart] == '\r' && dataStart + 1 < polyglotData.Length && polyglotData[dataStart + 1] == '\n')
                dataStart += 2;
            else if (polyglotData[dataStart] == '\n')
                dataStart += 1;
            // Else assume immediately follows (rare) or space
        }

        // Calculate data length handling newline before endstream
        var dataEnd = endStreamPos;
        if (dataEnd > dataStart && polyglotData[dataEnd - 1] == '\n')
        {
            dataEnd--;
            if (dataEnd > dataStart && polyglotData[dataEnd - 1] == '\r')
                dataEnd--;
        }

        var dataLength = dataEnd - dataStart;
        if (dataLength <= 0) throw new Exception("Empty payload");

        var payloadData = new byte[dataLength];
        Buffer.BlockCopy(polyglotData, dataStart, payloadData, 0, dataLength);

        return BinaryHelper.ExtractPayload(payloadData, password);
    }

    private int FindLastIndex(byte[] data, string pattern, int endIndex = -1)
    {
        var patternBytes = Encoding.ASCII.GetBytes(pattern);
        if (endIndex == -1 || endIndex > data.Length) endIndex = data.Length;
        
        for (int i = endIndex - patternBytes.Length; i >= 0; i--)
        {
            bool found = true;
            for (int j = 0; j < patternBytes.Length; j++)
            {
                if (data[i + j] != patternBytes[j])
                {
                    found = false;
                    break;
                }
            }
            if (found) return i;
        }
        return -1;
    }

    private int FindPdfEnd(byte[] data)
    {
        var pos = FindLastIndex(data, "%%EOF");
        return pos != -1 ? pos + 5 : -1;
    }

    private int GetNextObjectNumber(byte[] pdfData)
    {
        // Simple heuristic: scan for highest object number
        var text = Encoding.ASCII.GetString(pdfData);
        var matches = System.Text.RegularExpressions.Regex.Matches(text, @"(\d+)\s+\d+\s+obj");
        
        int maxObj = 0;
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (int.TryParse(match.Groups[1].Value, out int objNum))
            {
                maxObj = Math.Max(maxObj, objNum);
            }
        }
        
        return maxObj + 1;
    }

    private byte[] CreateStreamObject(int objectNumber, byte[] data)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{objectNumber} 0 obj");
        sb.AppendLine("<<");
        sb.AppendLine($"/Length {data.Length}");
        sb.AppendLine(">>");
        sb.AppendLine("stream");
        
        var header = Encoding.ASCII.GetBytes(sb.ToString());
        var footer = Encoding.ASCII.GetBytes("\nendstream\nendobj");

        var result = new byte[header.Length + data.Length + footer.Length];
        Buffer.BlockCopy(header, 0, result, 0, header.Length);
        Buffer.BlockCopy(data, 0, result, header.Length, data.Length);
        Buffer.BlockCopy(footer, 0, result, header.Length + data.Length, footer.Length);

        return result;
    }
}
