[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Executable,
    [Parameter(Mandatory)]
    [string]$NormalDataDirectory,
    [Parameter(Mandatory)]
    [string]$LargeDataDirectory,
    [Parameter(Mandatory)]
    [string]$Target,
    [int]$Runs = 5
)

$ErrorActionPreference = 'Stop'
$env:ICONFLOW_QA_AUTO_EXIT_MS = '1800'

function Measure-IconFlow {
    param(
        [string]$Mode,
        [string]$DataDirectory,
        [int]$Run
    )

    $ready = Join-Path $PSScriptRoot ".qa-$Mode-$Run.json"
    if (Test-Path -LiteralPath $ready) {
        Remove-Item -LiteralPath $ready -Force
    }

    $env:ICONFLOW_DATA_DIR = $DataDirectory
    $env:ICONFLOW_QA_READY_FILE = $ready
    if ($Mode.StartsWith('quick', [StringComparison]::OrdinalIgnoreCase)) {
        $process = Start-Process -FilePath $Executable -ArgumentList @('--change-icon', ('"' + $Target + '"')) -PassThru
    }
    else {
        $process = Start-Process -FilePath $Executable -PassThru
    }

    $deadline = (Get-Date).AddSeconds(8)
    while (-not (Test-Path -LiteralPath $ready) -and (Get-Date) -lt $deadline -and -not $process.HasExited) {
        Start-Sleep -Milliseconds 20
        $process.Refresh()
    }
    if (-not (Test-Path -LiteralPath $ready)) {
        throw "$Mode did not become ready."
    }

    $payload = Get-Content -LiteralPath $ready -Raw | ConvertFrom-Json
    $liveProcess = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
    $privateMb = if ($liveProcess) { [math]::Round($liveProcess.PrivateMemorySize64 / 1MB, 1) } else { $null }
    if (-not $process.HasExited) {
        Wait-Process -Id $process.Id -Timeout 5 -ErrorAction SilentlyContinue
    }

    [pscustomobject]@{
        Mode = $Mode
        Run = $Run
        ReadyMs = [int]$payload.readyMs
        PrivateMB = $privateMb
    }
}

$results = @()
1..$Runs | ForEach-Object { $results += Measure-IconFlow -Mode 'main' -DataDirectory $NormalDataDirectory -Run $_ }
1..$Runs | ForEach-Object { $results += Measure-IconFlow -Mode 'quick' -DataDirectory $NormalDataDirectory -Run $_ }
$results += Measure-IconFlow -Mode 'main-10k' -DataDirectory $LargeDataDirectory -Run 1
$results += Measure-IconFlow -Mode 'quick-10k' -DataDirectory $LargeDataDirectory -Run 1

$results | Format-Table -AutoSize
$results | ConvertTo-Json -Compress
