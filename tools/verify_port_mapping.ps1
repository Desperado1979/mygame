param(
    [Parameter(Mandatory = $true)]
    [string]$PublicIp,
    [int]$Port = 7777,
    [switch]$Udp = $true
)

$ErrorActionPreference = "Stop"

Write-Host "Target: $PublicIp`:$Port"

Write-Host "Testing TCP connectivity..." -ForegroundColor Cyan
$tcp = Test-NetConnection -ComputerName $PublicIp -Port $Port -InformationLevel Detailed
if ($tcp.TcpTestSucceeded) {
    Write-Host "TCP reachable: YES" -ForegroundColor Green
} else {
    Write-Host "TCP reachable: NO" -ForegroundColor Red
}

if ($Udp) {
    Write-Host "UDP cannot be fully verified by Test-NetConnection alone." -ForegroundColor Yellow
    Write-Host "Use in-game client connect as final UDP validation." -ForegroundColor Yellow
}

Write-Host "`nIf TCP fails:"
Write-Host "1) Check router NAT/port forward to server LAN IP"
Write-Host "2) Check Windows firewall inbound rules"
Write-Host "3) Confirm server process is listening on expected port"
