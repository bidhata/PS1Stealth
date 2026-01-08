# Build script for PS1Stealth

Write-Host "Building PS1Stealth..." -ForegroundColor Cyan

# Build in Release mode
Write-Host "[*] Building Release configuration..." -ForegroundColor Yellow
dotnet build -c Release

if ($LASTEXITCODE -eq 0) {
    Write-Host "[+] Build successful!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Executable location:" -ForegroundColor Cyan
    Write-Host "  .\bin\Release\net8.0-windows\PhotoViewer.exe" -ForegroundColor White
    Write-Host ""
    Write-Host "To publish as single file:" -ForegroundColor Cyan
    Write-Host "  .\PUBLISH.ps1" -ForegroundColor White
    Write-Host ""
    Write-Host "To run demo:" -ForegroundColor Cyan
    Write-Host "  .\DEMO.ps1" -ForegroundColor White
}
else {
    Write-Host "[!] Build failed!" -ForegroundColor Red
    exit 1
}
