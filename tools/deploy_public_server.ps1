param(
    [Parameter(Mandatory = $true)]
    [string]$ExePath,
    [int]$Port = 7777,
    [switch]$OpenFirewall = $true
)

$ErrorActionPreference = "Stop"

if (!(Test-Path $ExePath)) {
    throw "Exe not found: $ExePath"
}

Write-Host "Starting dedicated server..." -ForegroundColor Cyan
Write-Host "Exe: $ExePath"
Write-Host "Port: $Port"

if ($OpenFirewall) {
    Write-Host "Ensuring firewall inbound rules for TCP/UDP $Port..." -ForegroundColor Yellow
    $ruleTcp = "EpochOfDawn_Server_TCP_$Port"
    $ruleUdp = "EpochOfDawn_Server_UDP_$Port"

    if (-not (Get-NetFirewallRule -DisplayName $ruleTcp -ErrorAction SilentlyContinue)) {
        New-NetFirewallRule -DisplayName $ruleTcp -Direction Inbound -Protocol TCP -LocalPort $Port -Action Allow | Out-Null
    }
    if (-not (Get-NetFirewallRule -DisplayName $ruleUdp -ErrorAction SilentlyContinue)) {
        New-NetFirewallRule -DisplayName $ruleUdp -Direction Inbound -Protocol UDP -LocalPort $Port -Action Allow | Out-Null
    }
}

Write-Host "Command:" -ForegroundColor Green
Write-Host "`"$ExePath`" -batchmode -nographics -server -port $Port"

& $ExePath -batchmode -nographics -server -port $Port
