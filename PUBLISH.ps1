# Publish script - Creates single-file executable

Write-Host "Publishing PS1Stealth as single-file executable..." -ForegroundColor Cyan
Write-Host ""

$runtime = "win-x64"
$outputPath = ".\publish"

Write-Host "[*] Target Runtime: $runtime" -ForegroundColor Yellow
Write-Host "[*] Output Path: $outputPath" -ForegroundColor Yellow
Write-Host ""

# Publish as self-contained single file
dotnet publish -c Release `
    -r $runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishTrimmed=false `
    -o $outputPath

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "[+] Publish successful!" -ForegroundColor Green
    Write-Host ""
    
    $exePath = Join-Path $outputPath "PhotoViewer.exe"
    if (Test-Path $exePath) {
        $size = (Get-Item $exePath).Length
        $sizeMB = [math]::Round($size / 1MB, 2)
        
        Write-Host "Executable created:" -ForegroundColor Cyan
        Write-Host "  Path: $exePath" -ForegroundColor White
        Write-Host "  Size: $sizeMB MB" -ForegroundColor White
        Write-Host ""
        Write-Host "This is a standalone executable that requires no dependencies!" -ForegroundColor Green
        Write-Host ""
        Write-Host "For even smaller size, consider:" -ForegroundColor Yellow
        Write-Host "  • Using .NET Native AOT compilation" -ForegroundColor DarkGray
        Write-Host "  • Enabling PublishTrimmed (may break reflection)" -ForegroundColor DarkGray
        Write-Host "  • Using UPX compression (upx --best PhotoViewer.exe)" -ForegroundColor DarkGray
    }
}
else {
    Write-Host ""
    Write-Host "[!] Publish failed!" -ForegroundColor Red
    exit 1
}
