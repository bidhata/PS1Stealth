namespace PS1Stealth.Core;

public class PayloadData
{
    public string ScriptContent { get; set; } = string.Empty;
    public string? Password { get; set; }
    public bool UseCompression { get; set; } = true;
    public ObfuscationLevel ObfuscationLevel { get; set; } = ObfuscationLevel.None;
    public bool AddAMSIBypass { get; set; } = false;
    public bool PrepareForInMemory { get; set; } = false;
}

public class ExecutionResult
{
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public bool Success { get; set; }
}
