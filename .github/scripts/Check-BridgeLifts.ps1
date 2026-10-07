[CmdletBinding()]
param(
    [string] $BaseUrl = 'http://localhost:8080',
    [int]    $MinLifts = 1
)

$ErrorActionPreference = 'Stop'

function ConvertTo-LiftDate {
    param([object] $Value)
    # ConvertFrom-Json turns ISO 8601 strings into [datetime]; accept both that and a raw string.
    if ($Value -is [datetime]) { return $Value }
    $parsed = [datetime]::MinValue
    if ([datetime]::TryParse([string]$Value, [System.Globalization.CultureInfo]::InvariantCulture, [System.Globalization.DateTimeStyles]::RoundtripKind, [ref]$parsed)) {
        return $parsed
    }
    throw "Unparsable date '$Value'"
}

Write-Host 'Waiting for API to become ready...'
$ready = $false
for ($i = 0; $i -lt 60; $i++) {
    try {
        Invoke-WebRequest "$BaseUrl/swagger/index.html" -UseBasicParsing -TimeoutSec 5 | Out-Null
        $ready = $true
        break
    } catch {
        Start-Sleep -Seconds 1
    }
}
if (-not $ready) { throw "API did not become ready at $BaseUrl" }

$response = $null
for ($i = 1; $i -le 3; $i++) {
    try {
        $response = Invoke-WebRequest "$BaseUrl/api/bridgelifts" -UseBasicParsing -TimeoutSec 45
        break
    } catch {
        Write-Warning "Attempt $i failed: $($_.Exception.Message)"
        Start-Sleep -Seconds 5
    }
}

if (-not $response)               { throw 'GET /api/bridgelifts did not return a response' }
if ($response.StatusCode -ne 200) { throw "Expected HTTP 200, got $($response.StatusCode)" }

$lifts = @($response.Content | ConvertFrom-Json)

if ($lifts.Count -lt $MinLifts) {
    throw "Parsed $($lifts.Count) lifts (expected >= $MinLifts) -- selectors probably no longer match. Body: $($response.Content)"
}

$dates = @()
foreach ($lift in $lifts) {
    if ([string]::IsNullOrWhiteSpace($lift.vessel)) {
        throw "Lift missing a vessel. Body: $($response.Content)"
    }
    if ($lift.direction -notin 'UpRiver', 'DownRiver', 'Unknown') {
        throw "Unexpected direction '$($lift.direction)'"
    }
    $dates += ConvertTo-LiftDate $lift.date
}

$withDirection = @($lifts | Where-Object { $_.direction -ne 'Unknown' }).Count
if ($withDirection -eq 0) {
    throw "Every one of $($lifts.Count) lifts has direction 'Unknown' -- direction parsing probably broke"
}

$upcoming = @($dates | Where-Object { $_ -ge (Get-Date).ToUniversalTime().AddHours(-12) }).Count
if ($upcoming -eq 0) { Write-Warning 'No upcoming lifts -- timetable may be empty or dates stale' }

Write-Host "OK: $($lifts.Count) lifts parsed, $withDirection with a direction, $upcoming upcoming."
