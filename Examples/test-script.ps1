# Example PowerShell script for testing
Write-Host "==================================="
Write-Host "  PS1Stealth Test Script"
Write-Host "==================================="
Write-Host ""

# System Information
Write-Host "[+] System Information:"
Write-Host "  Computer Name: $env:COMPUTERNAME"
Write-Host "  Username: $env:USERNAME"
Write-Host "  OS: $(Get-WmiObject Win32_OperatingSystem | Select-Object -ExpandProperty Caption)"
Write-Host ""

# Network Information
Write-Host "[+] Network Configuration:"
Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.InterfaceAlias -notlike "*Loopback*" } | Select-Object InterfaceAlias, IPAddress | Format-Table

# Running Processes (top 10 by CPU)
Write-Host "[+] Top 10 Processes by CPU:"
Get-Process | Sort-Object CPU -Descending | Select-Object -First 10 Name, CPU, WorkingSet | Format-Table

Write-Host ""
Write-Host "[+] Script execution completed successfully!"
Write-Host "==================================="
