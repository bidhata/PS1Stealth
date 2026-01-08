namespace PS1Stealth.Core;

public interface IEmbedder
{
    Task<byte[]> EmbedAsync(byte[] carrierData, PayloadData payload);
    Task<string> ExtractAsync(byte[] polyglotData, string? password = null);
}
