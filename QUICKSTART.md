# Quick Start Guide

## Step 1: Build the Tool

```powershell
cd PS1Stealth
dotnet build -c Release
```

## Step 2: Create a Test Script

```powershell
@"
Write-Host 'This is a hidden PowerShell script!'
Write-Host 'Executed at: ' (Get-Date)
Write-Host 'Computer: ' `$env:COMPUTERNAME
"@ | Out-File test.ps1
```

## Step 3: Get a Carrier File

Use any PNG image, PDF document, or Office file. For quick testing:

```powershell
# Download a test image
Invoke-WebRequest -Uri "https://via.placeholder.com/800x600.png" -OutFile "carrier.png"
```

Or create one programmatically:

```powershell
Add-Type -AssemblyName System.Drawing
$bmp = New-Object System.Drawing.Bitmap(800, 600)
$graphics = [System.Drawing.Graphics]::FromImage($bmp)
$graphics.Clear([System.Drawing.Color]::CornflowerBlue)
$graphics.Dispose()
$bmp.Save("carrier.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Host "Created carrier.png"
```

## Step 4: Embed Your Script

```powershell
cd bin\Release\net8.0-windows\

# Embed with encryption
.\PS1Stealth.exe embed ..\..\..\test.ps1 carrier.png output.png --method ImageLSB --password "MySecret123"
```

## Step 5: Verify

The output file should:
- ✅ Look identical to the original (for images)
- ✅ Open normally in its native application
- ✅ Contain hidden PowerShell payload

```powershell
# View the image - should look normal
Start-Process output.png

# Check file size (will be slightly larger)
(Get-Item carrier.png).Length
(Get-Item output.png).Length
```

## Step 6: Extract or Execute

### Option A: Extract to file
```powershell
.\PS1Stealth.exe extract output.png extracted.ps1 --method ImageLSB --password "MySecret123"

# View extracted script
Get-Content extracted.ps1
```

### Option B: Execute directly (in-memory, no file written)
```powershell
.\PS1Stealth.exe execute output.png --method ImageLSB --password "MySecret123"
```

## Next Steps

- Read [USAGE_EXAMPLES.md](Examples/USAGE_EXAMPLES.md) for advanced scenarios
- Explore different embedding methods
- Try with PDF and Office documents
- Read the [README.md](README.md) for complete documentation

## Common Commands Cheat Sheet

```powershell
# Show all methods
.\PS1Stealth.exe info

# Embed in PNG with encryption
.\PS1Stealth.exe embed script.ps1 image.png output.png --method ImageLSB --password "pass"

# Embed in PDF
.\PS1Stealth.exe embed script.ps1 doc.pdf out.pdf --method PdfPolyglot --password "pass"

# Embed in Office document
.\PS1Stealth.exe embed script.ps1 doc.docx out.docx --method ZipComment --password "pass"

# Execute with AMSI bypass
.\PS1Stealth.exe execute output.png --method ImageLSB --password "pass" --bypass-amsi

# Extract
.\PS1Stealth.exe extract output.png script.ps1 --method ImageLSB --password "pass"
```

## Troubleshooting

**Build errors?**
```powershell
# Make sure .NET 8.0 SDK is installed
dotnet --version

# Should show 8.0.x or higher
```

**"Image too small" error?**
- Use a larger image (need ~8 pixels per byte)
- Enable compression: `--compress true`
- Or use a different method like `ImagePolyglot`

**Can't execute?**
- Make sure password matches what you used for embedding
- Check that method parameter matches
- Verify file isn't corrupted

---

**You're ready to go! Happy (ethical) hacking! 🔒**
