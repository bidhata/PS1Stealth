using PS1Stealth.Core;
using System.Text;

namespace PS1Stealth.Embedders;

/// <summary>
/// Embeds payload in MP4 using atom manipulation - inspired by beheader's technique
/// Creates a "skip" atom that video players ignore but contains our payload
/// </summary>
public class Mp4AtomEmbedder : IEmbedder
{
    private static readonly byte[] FtypSignature = Encoding.ASCII.GetBytes("ftyp");
    private static readonly byte[] SkipSignature = Encoding.ASCII.GetBytes("skip");
    private static readonly byte[] FreeSignature = Encoding.ASCII.GetBytes("free");

    public async Task<byte[]> EmbedAsync(byte[] carrierData, PayloadData payload)
    {
        // Validate it's an MP4 file
        if (!IsValidMp4(carrierData))
            throw new Exception("Carrier file is not a valid MP4 file");

        // Prepare payload
        var payloadBytes = BinaryHelper.PreparePayload(
            payload.ScriptContent,
            payload.Password,
            payload.UseCompression);

        // Create a "skip" atom containing the payload
        // Format: [size (4 bytes BE)][type "skip" (4 bytes)][payload data]
        var atomSize = 8 + payloadBytes.Length; // 4 (size) + 4 (type) + payload

        var skipAtom = new byte[atomSize];
        
        // Write atom size (big-endian)
        var sizeBytes = BinaryHelper.ToBigEndian32(atomSize);
        Buffer.BlockCopy(sizeBytes, 0, skipAtom, 0, 4);
        
        // Write atom type "skip"
        Buffer.BlockCopy(SkipSignature, 0, skipAtom, 4, 4);
        
        // Write payload
        Buffer.BlockCopy(payloadBytes, 0, skipAtom, 8, payloadBytes.Length);

        // SAFER APPROACH: Append skip atom at the END of the file
        // This preserves all existing MP4 structure and won't break playback
        // Video players will read the video normally and ignore the trailing skip atom
        
        var output = new byte[carrierData.Length + skipAtom.Length];
        
        // Copy entire original file
        Buffer.BlockCopy(carrierData, 0, output, 0, carrierData.Length);
        
        // Append skip atom at the end
        Buffer.BlockCopy(skipAtom, 0, output, carrierData.Length, skipAtom.Length);

        return output;
    }

    public async Task<string> ExtractAsync(byte[] polyglotData, string? password = null)
    {
        // Find all skip/free atoms in the file
        var atoms = FindAtoms(polyglotData, SkipSignature);
        
        if (atoms.Count == 0)
        {
            // Try "free" atoms as fallback
            atoms = FindAtoms(polyglotData, FreeSignature);
        }

        if (atoms.Count == 0)
            throw new Exception("No embedded payload found in MP4 file");

        // Try each atom until we find one with valid PS1X signature
        Exception? lastException = null;
        
        foreach (var atom in atoms)
        {
            try
            {
                var atomData = new byte[atom.Size - 8]; // Size includes the 8-byte header
                Buffer.BlockCopy(polyglotData, atom.Offset + 8, atomData, 0, atomData.Length);
                
                // Try to extract payload from this atom
                return BinaryHelper.ExtractPayload(atomData, password);
            }
            catch (Exception ex)
            {
                lastException = ex;
                // Continue trying other atoms
            }
        }

        throw new Exception($"No valid payload found in skip/free atoms. Last error: {lastException?.Message}");
    }

    private bool IsValidMp4(byte[] data)
    {
        if (data.Length < 12)
            return false;

        // Check for ftyp atom (usually at the beginning)
        // Format: [size][ftyp][brand][version]
        for (int i = 0; i <= Math.Min(data.Length - 12, 256); i++)
        {
            if (data[i] == 'f' && data[i + 1] == 't' && 
                data[i + 2] == 'y' && data[i + 3] == 'p')
            {
                return true;
            }
        }

        return false;
    }

    private int FindInsertionPoint(byte[] mp4Data)
    {
        // Find the end of ftyp atom
        var ftypIndex = FindAtomIndex(mp4Data, FtypSignature);
        
        if (ftypIndex == -1)
        {
            // If no ftyp found, insert at beginning
            return 0;
        }

        // Read ftyp atom size
        var ftypSize = BinaryHelper.FromBigEndian32(mp4Data, ftypIndex);
        
        // Insert right after ftyp atom
        return ftypIndex + ftypSize;
    }

    private int FindAtomIndex(byte[] data, byte[] signature)
    {
        for (int i = 0; i <= data.Length - signature.Length - 4; i++)
        {
            // MP4 atoms have format: [4 bytes size][4 bytes type]
            // We're looking for the type, which starts at position i+4
            bool match = true;
            for (int j = 0; j < signature.Length; j++)
            {
                if (data[i + 4 + j] != signature[j])
                {
                    match = false;
                    break;
                }
            }
            
            if (match)
            {
                return i; // Return the position of the size field
            }
        }
        
        return -1;
    }

    private List<Mp4Atom> FindAtoms(byte[] data, byte[] atomType)
    {
        var atoms = new List<Mp4Atom>();
        
        int i = 0;
        while (i < data.Length - 8)
        {
            try
            {
                // Read atom size (big-endian)
                var atomSize = BinaryHelper.FromBigEndian32(data, i);
                
                // Sanity check
                if (atomSize < 8 || atomSize > data.Length - i)
                {
                    i++;
                    continue;
                }

                // Check if this is the atom type we're looking for
                bool match = true;
                for (int j = 0; j < atomType.Length; j++)
                {
                    if (data[i + 4 + j] != atomType[j])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    atoms.Add(new Mp4Atom
                    {
                        Offset = i,
                        Size = atomSize,
                        Type = Encoding.ASCII.GetString(atomType)
                    });
                }

                // Move to next atom
                i += atomSize;
            }
            catch
            {
                i++;
            }
        }

        return atoms;
    }

    private class Mp4Atom
    {
        public int Offset { get; set; }
        public int Size { get; set; }
        public string Type { get; set; } = string.Empty;
    }
}
