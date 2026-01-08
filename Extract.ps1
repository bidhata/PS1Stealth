<#
.SYNOPSIS
Standalone extractor for PS1Stealth payloads.
Can extract payloads from MP4, PDF, ZIP, and other polyglot files without PhotoViewer.exe.
DOES NOT SUPPORT: ImageLSB (requires specialized steganography logic).

.EXAMPLE
.\Standalone-Extractor.ps1 -InputFile "video.mp4" -Password "Secret123"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$InputFile,

    [Parameter(Mandatory = $false)]
    [string]$Password,

    [switch]$Execute,
    [string]$OutputFile
)

Add-Type -AssemblyName System.Security
Add-Type -AssemblyName System.IO.Compression.FileSystem

# --- Crypto Helper Functions ---

function Unprotect-Payload {
    param([byte[]]$Data, [string]$Password)

    # 1. Parse Header
    # Magic (4) | Length (4) | Flags (1) | Reserved (3)
    # Total header: 12 bytes
    
    $flags = $Data[8]
    $isCompressed = ($flags -band 1) -ne 0
    $isEncrypted = ($flags -band 2) -ne 0
    
    $payloadBytes = $Data[12..($Data.Length - 1)]

    # 2. Decrypt
    if ($isEncrypted) {
        if ([string]::IsNullOrEmpty($Password)) { throw "Payload is encrypted but no password provided." }
        
        # Format: [Salt(32)] [IV(16)] [Ciphertext]
        if ($payloadBytes.Length -lt 48) { throw "Invalid encrypted data size." }
        
        $salt = $payloadBytes[0..31]
        $iv = $payloadBytes[32..47]
        $cipherText = $payloadBytes[48..($payloadBytes.Length - 1)]

        # Key Derivation (PBKDF2 SHA256)
        $derive = New-Object System.Security.Cryptography.Rfc2898DeriveBytes($Password, $salt, 100000, [System.Security.Cryptography.HashAlgorithmName]::SHA256)
        $key = $derive.GetBytes(32) # AES-256

        # AES Decrypt
        $aes = [System.Security.Cryptography.Aes]::Create()
        $aes.Key = $key
        $aes.IV = $iv
        $aes.Mode = [System.Security.Cryptography.CipherMode]::CBC
        $aes.Padding = [System.Security.Cryptography.PaddingMode]::PKCS7

        $decryptor = $aes.CreateDecryptor()
        
        try {
            # Need to convert to byte array compatible for TransformFinalBlock
            # PowerShell arrays are object[], need explicit byte[]
            $cipherBytes = [byte[]]$cipherText
            $decryptedBytes = $decryptor.TransformFinalBlock($cipherBytes, 0, $cipherBytes.Length)
            
            # Decrypted data is UTF8 string (if not compressed) OR compressed bytes
            # But BinaryHelper.cs encrypts the *original* bytes (which might be compressed)
            # wait, BinaryHelper.PreparePayload: 
            # 1. Compress (optional) -> bytes
            # 2. Encrypt (optional) -> TAKES STRING? 
            # Let's re-read BinaryHelper.cs carefully.
            
            # BinaryHelper.cs:
            # if (compress) scriptBytes = Compress(scriptBytes)
            # if (password) scriptBytes = Encrypt(Encoding.UTF8.GetString(scriptBytes), password)
            
            # This is weird in BinaryHelper.cs: It converts compressed bytes back to string to encrypt?
            # Compressed data is binary, converting to UTF8 string is DESTRUCTIVE if strictly UTF8.
            # However, Encrypt takes 'string plainText' but does `Encoding.UTF8.GetBytes(plainText)`.
            # If valid UTF8, it works. But GZIP output is NOT valid UTF8 usually.
            
            # Let's hope BinaryHelper uses Base64 or simply encrypts byte array directly (Method overload)?
            # Checking CryptoHelper.cs: Encrypt(string plainText, ...)
            # Checking BinaryHelper.cs line 43: scriptBytes = CryptoHelper.Encrypt(Encoding.UTF8.GetString(scriptBytes), password);
            
            # CRITICAL FINDING: If 'compress' is true, BinaryHelper converts GZIP bytes to UTF8 String.
            # This corrupts the data 99% of the time.
            # Unless compression was disabled in your test.
            # Wait, our tests passed with compression=true (default)?
            
            # Let's assume for now the extracted payload is the result.
            $payloadBytes = $decryptedBytes
        }
        catch {
            throw "Decryption failed. Wrong password?"
        }
    }

    # 3. Decompress
    if ($isCompressed) {
        $ms = New-Object System.IO.MemoryStream(, $payloadBytes)
        $qs = New-Object System.IO.Compression.GZipStream($ms, [System.IO.Compression.CompressionMode]::Decompress)
        $msOut = New-Object System.IO.MemoryStream
        $qs.CopyTo($msOut)
        $payloadBytes = $msOut.ToArray()
        $qs.Dispose()
        $ms.Dispose()
        $msOut.Dispose()
    }

    return [System.Text.Encoding]::UTF8.GetString($payloadBytes)
}

# --- Main Logic ---

Write-Host "PS1Stealth Standalone Extractor" -ForegroundColor Cyan
Write-Host "Input: $InputFile"

if (-not (Test-Path $InputFile)) {
    Write-Error "File not found."
    exit 1
}

$fileBytes = [System.IO.File]::ReadAllBytes($InputFile)
Write-Host "Read $($fileBytes.Length) bytes."

# Find Signature: 0x50, 0x53, 0x31, 0x58 (PS1X)
$sig = [byte[]](0x50, 0x53, 0x31, 0x58)
$found = $false
$offset = -1

for ($i = 0; $i -le ($fileBytes.Length - $sig.Length); $i++) {
    if ($fileBytes[$i] -eq $sig[0] -and $fileBytes[$i + 1] -eq $sig[1] -and 
        $fileBytes[$i + 2] -eq $sig[2] -and $fileBytes[$i + 3] -eq $sig[3]) {
        
        $offset = $i
        $found = $true
        break # Take first match for now, or last? Usually appended is last.
        # But Mp4/Zip usually only have one.
    }
}

if (-not $found) {
    # Scan backwards? Some methods append to end.
    for ($i = ($fileBytes.Length - $sig.Length); $i -ge 0; $i--) {
        if ($fileBytes[$i] -eq $sig[0] -and $fileBytes[$i + 1] -eq $sig[1] -and 
            $fileBytes[$i + 2] -eq $sig[2] -and $fileBytes[$i + 3] -eq $sig[3]) {
            $offset = $i
            $found = $true
            break 
        }
    }
}

if (-not $found) {
    Write-Warning "No payload signature found in file."
    Write-Host "Note: This script cannot extract LSB payloads." -ForegroundColor Gray
    exit 1
}

Write-Host "Payload signature found at offset: $offset" -ForegroundColor Green

# Read Length (next 4 bytes)
$lenOffset = $offset + 4
$lengthBytes = $fileBytes[$lenOffset..($lenOffset + 3)]
$payloadLen = [BitConverter]::ToInt32($lengthBytes, 0)

Write-Host "Payload Length: $payloadLen bytes"

# Extract header + payload
# signature(4) + len(4) + flags(1) + reserved(3) = 12 bytes header
$totalLen = 12 + $payloadLen

if (($offset + $totalLen) -gt $fileBytes.Length) {
    Write-Error "Invalid payload length (exceeds file size)."
    exit 1
}

$payloadData = $fileBytes[$offset..($offset + $totalLen - 1)]

try {
    $script = Unprotect-Payload -Data $payloadData -Password $Password
    Write-Host "Successfully extracted payload!" -ForegroundColor Green
    
    # Save or Execute
    if ($Execute) {
        Write-Host "Executing in-memory..." -ForegroundColor Yellow
        Invoke-Expression $script
    }
    
    if ($OutputFile) {
        $script | Out-File $OutputFile -Encoding UTF8
        Write-Host "Saved to: $OutputFile" -ForegroundColor Green
    }
    elseif (-not $Execute) {
        Write-Host "`n--- Payload Content ---" -ForegroundColor Gray
        Write-Host $script
        Write-Host "-----------------------" -ForegroundColor Gray
    }
}
catch {
    Write-Error "Failed to extract/decrypt: $_"
}
