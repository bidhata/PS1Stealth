# PS1Stealth - Project Summary

## 🎯 What Was Created

A complete C# Red Teaming tool for hiding PowerShell scripts in various file formats, inspired by the [beheader](https://github.com/p2r3/beheader) polyglot generator.

## 📁 Project Structure

```
PS1Stealth/
│
├── 📄 PS1Stealth.csproj          # Project file
├── 📄 Program.cs                 # Main CLI application
├── 📄 README.md                  # Complete documentation
├── 📄 QUICKSTART.md              # Quick start guide
├── 📄 BUILD.ps1                  # Build script
├── 📄 PUBLISH.ps1                # Publish script (single-file exe)
├── 📄 DEMO.ps1                   # Interactive demo
│
├── 📂 Core/                      # Core functionality
│   ├── IEmbedder.cs             # Embedder interface
│   ├── PayloadData.cs           # Data models
│   ├── CryptoHelper.cs          # AES-256 encryption & compression
│   └── BinaryHelper.cs          # Binary manipulation
│
├── 📂 Embedders/                 # Embedding implementations
│   ├── ImageLSBEmbedder.cs      # LSB steganography in images
│   ├── ImagePolyglotEmbedder.cs # ICO polyglot files
│   ├── PdfPolyglotEmbedder.cs   # PDF stream injection
│   ├── ZipCommentEmbedder.cs    # ZIP comment field
│   └── IcoAtomEmbedder.cs       # ICO header manipulation
│
├── 📂 Executors/                 # Execution engine
│   └── PowerShellExecutor.cs    # In-memory PS execution
│
└── 📂 Examples/                  # Documentation & examples
    ├── test-script.ps1          # Sample test script
    └── USAGE_EXAMPLES.md        # Detailed usage examples
```

## 🚀 Key Features

### 5 Embedding Methods

1. **ImageLSB** - Steganography in image pixels
2. **ImagePolyglot** - ICO format polyglots
3. **PdfPolyglot** - PDF stream objects
4. **ZipComment** - ZIP comment field (works with DOCX, XLSX, JAR, APK)
5. **IcoAtom** - ICO header manipulation

### Security Features

- ✅ **AES-256 Encryption** with PBKDF2 key derivation
- ✅ **GZip Compression** to reduce payload size
- ✅ **In-Memory Execution** - no disk writes
- ✅ **AMSI Bypass** capability (educational)
- ✅ **Magic Signature** for payload identification

## 🔧 How to Use

### Build

```powershell
cd PS1Stealth
.\BUILD.ps1
```

### Run Demo

```powershell
.\DEMO.ps1
```

### Basic Usage

```powershell
# Embed a script
.\bin\Release\net8.0-windows\PhotoViewer.exe embed script.ps1 image.png output.png --method ImageLSB --password "secret"

# Extract a script
.\bin\Release\net8.0-windows\PhotoViewer.exe extract output.png extracted.ps1 --method ImageLSB --password "secret"

# Execute directly (in-memory)
.\bin\Release\net8.0-windows\PhotoViewer.exe execute output.png --method ImageLSB --password "secret"
```

## 🎓 Inspired by Beheader

### Key Differences

| Aspect | Beheader | PS1Stealth |
|--------|----------|------------|
| **Language** | JavaScript (Bun) | C# (.NET 8.0) |
| **Platform** | Linux | Windows |
| **Purpose** | Media polyglots | PowerShell payloads |
| **Formats** | ICO+MP4+HTML+PDF+ZIP | PNG+ICO+PDF+ZIP+Office |
| **Encryption** | ❌ No | ✅ AES-256 |
| **Execution** | ❌ No | ✅ In-memory PowerShell |
| **Dependencies** | ffmpeg, ImageMagick, mp4edit | ✅ None (.NET only) |

### Techniques Borrowed from Beheader

1. **Polyglot File Structure** - Files valid in multiple formats
2. **Header Manipulation** - Strategic use of format headers
3. **Skip/Free Space** - Using ignored sections for payload
4. **Offset Adjustment** - Updating file pointers
5. **Format Tolerance** - Exploiting parser differences

## 🔒 Security Considerations

### ⚠️ Legal Warning

**FOR AUTHORIZED TESTING ONLY**

This tool is for:
- Penetration testing with authorization
- Red Team exercises
- Security research
- Educational purposes

Unauthorized use is illegal!

### Detection Considerations

- **AV/EDR** - May detect known AMSI bypass patterns
- **Behavioral Analysis** - In-memory execution may be flagged
- **Network Monitoring** - C2 communication is still detectable
- **File Analysis** - Forensic tools may detect anomalies

### Operational Security

- Use encryption for all payloads
- Choose innocuous carrier files
- Test in controlled environments first
- Have cleanup procedures
- Document your activities
- Follow responsible disclosure

## 🛠️ Technical Architecture

### Payload Format

```
┌──────────────────────────────────────┐
│ Magic Signature: "PS1X" (4 bytes)   │
├──────────────────────────────────────┤
│ Data Length: int32 (4 bytes)        │
├──────────────────────────────────────┤
│ Flags: byte (1 byte)                │
│   Bit 0: Compressed                 │
│   Bit 1: Encrypted                  │
├──────────────────────────────────────┤
│ Reserved: (3 bytes)                 │
├──────────────────────────────────────┤
│ Payload Data: variable              │
│ [Optional] Salt (32 bytes)          │
│ [Optional] IV (16 bytes)            │
│ [Content]                           │
└──────────────────────────────────────┘
```

### Encryption Flow

```
PowerShell Script
      ↓
[Optional] GZip Compression
      ↓
[Optional] AES-256-!CBC Encryption
  • PBKDF2 key derivation (100k iterations)
  • Random 32-byte salt
  • Random 16-byte IV
      ↓
Add PS1X Header
      ↓
Embed in Carrier File
```

### Extraction Flow

```
Polyglot File
      ↓
Locate PS1X Signature
      ↓
Parse Header & Extract Payload
      ↓
[If Encrypted] AES-256 Decrypt
      ↓
[If Compressed] GZip Decompress
      ↓
PowerShell Script
```

## 📊 Capabilities Matrix

| Method | Formats | Capacity | Detectability | Use Case |
|--------|---------|----------|---------------|----------|
| **ImageLSB** | PNG, BMP | High | Low | Best for large payloads |
| **ImagePolyglot** | ICO | High | Low | ICO files with data |
| **PdfPolyglot** | PDF | Medium | Medium | Document-based delivery |
| **ZipComment** | ZIP, DOCX, XLSX, JAR, APK | Medium | Low | Office docs |
| **IcoAtom** | ICO | Low-High | Low | Icon files |

## 🎯 Use Cases

### Red Team Operations

1. **Initial Access** - Hide payloads in phishing attachments
2. **Lateral Movement** - Distribute via shared drives
3. **Persistence** - Embed in system images
4. **Exfiltration** - Hide data in outbound files
5. **Command & Control** - Beacon scripts in logos

### Security Testing

1. **AV/EDR Testing** - Test detection capabilities
2. **DLP Testing** - Test data loss prevention
3. **SIEM Testing** - Validate monitoring rules
4. **User Awareness** - Phishing simulations

### Research

1. **File Format Analysis** - Study polyglot techniques
2. **Steganography** - Explore hiding methods
3. **Obfuscation** - Test detection bypass

## 🚀 Next Steps

### For Users

1. Read `QUICKSTART.md` for first steps
2. Run `DEMO.ps1` for interactive demo
3. Study `Examples/USAGE_EXAMPLES.md` for scenarios
4. Read `README.md` for complete documentation

### For Developers

1. Implement custom `IEmbedder` for new formats
2. Add additional encryption methods
3. Integrate with C2 frameworks
4. Enhance obfuscation techniques

### Production Deployment

1. Build with `PUBLISH.ps1` for single-file exe
2. Consider code signing
3. Apply obfuscation (ConfuserEx, .NET Reactor)
4. Test against target AV/EDR

## 📚 Learning Resources

### Polyglot Files

- [PoC||GTFO](https://www.alchemistowl.org/pocorgtfo/) - Polyglot research
- [Beheader](https://github.com/p2r3/beheader) - Original inspiration
- [Corkami](https://github.com/corkami/pics) - File format posters

### PowerShell Security

- [PowerShell Obfuscation Bible](https://www.danielbohannon.com/blog-1/2017/3/5/powershell-obfuscation-bible)
- [AMSI Bypass](https://s3cur3th1ssh1t.github.io/Bypass_AMSI_by_manual_modification_part_1/)
- [Red Team Tactics](https://attack.mitre.org/)

### Steganography

- [LSB Steganography](https://en.wikipedia.org/wiki/Steganography)
- [Digital Watermarking](https://www.sciencedirect.com/topics/computer-science/digital-watermarking)

## 🤝 Contributing

Ideas for contributions:

- [ ] Add MP4 polyglot support (like beheader)
- [ ] Implement chunked encryption for large files
- [ ] Add alternative AMSI bypass techniques
- [ ] Create GUI interface
- [ ] Add network exfiltration capabilities
- [ ] Support Linux (using .NET Core)
- [ ] Add EXE polyglot support
- [ ] Implement advanced obfuscation

## 📝 License

MIT License - For educational and authorized testing only

## ⚖️ Ethics & Responsibility

This tool is powerful and can be misused. Always:

- ✅ Get written authorization before testing
- ✅ Document all activities
- ✅ Follow rules of engagement
- ✅ Report findings responsibly
- ✅ Respect privacy and laws
- ❌ Never use on unauthorized systems
- ❌ Never cause harm or damage
- ❌ Never violate privacy

**Remember**: With great power comes great responsibility. Use ethically!

---

## 🎉 Conclusion

You now have a complete, production-ready Red Teaming tool for hiding PowerShell payloads in various file formats using techniques inspired by beheader's polyglot approach.

The tool includes:
- ✅ 5 embedding methods
- ✅ Strong encryption (AES-256)
- ✅ In-memory execution
- ✅ Complete documentation
- ✅ Working examples
- ✅ Interactive demo
- ✅ Build scripts

**Ready to use for authorized security testing!**

---

Created with ❤️ for the security community
Inspired by [beheader](https://github.com/p2r3/beheader) by p2r3
