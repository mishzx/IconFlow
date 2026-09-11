[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$appProject = Join-Path $root 'native\IconFlow.WinUI\IconFlow.WinUI.csproj'
$testProject = Join-Path $root 'native\IconFlow.Native.Tests\IconFlow.Native.Tests.csproj'
$spriteSheet = Join-Path $root 'assets\builtin-icons\fluent-folder-family-v1.png'
$builtInOutput = Join-Path $root 'assets\builtin-icons\generated'

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $OutputDirectory = Join-Path $root "release-native\IconFlow-native-$stamp"
}

& (Join-Path $PSScriptRoot 'generate-localization.ps1')
dotnet run --project $testProject -c $Configuration -- --prepare-builtin $spriteSheet $builtInOutput
if ($LASTEXITCODE -ne 0) { throw 'Built-in icon generation failed.' }

dotnet clean $appProject -c $Configuration
if ($LASTEXITCODE -ne 0) { throw 'Clean failed.' }
dotnet build $appProject -c $Configuration
if ($LASTEXITCODE -ne 0) { throw 'WinUI build failed.' }
dotnet run --project $testProject -c $Configuration
if ($LASTEXITCODE -ne 0) { throw 'Core regression tests failed.' }
dotnet publish $appProject -c $Configuration -r win-x64 --self-contained false `
    -p:WindowsAppSDKSelfContained=false -p:PublishSingleFile=false -o $OutputDirectory
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }

& (Join-Path $PSScriptRoot 'build-shell-extension.ps1') -OutputDirectory $OutputDirectory
$packageHost = Join-Path $OutputDirectory 'IconFlow.PackageHost.exe'
Copy-Item -LiteralPath (Join-Path $OutputDirectory 'IconFlow.exe') -Destination $packageHost -Force
$sparseDir = Join-Path $env:TEMP ("IconFlow-sparse-" + [guid]::NewGuid().ToString('N'))
winapp init --exe $packageHost --sparse --output-dir $sparseDir --use-defaults --force
if ($LASTEXITCODE -ne 0) { throw 'Sparse identity initialization failed.' }
$manifest = Join-Path $sparseDir 'appxmanifest.xml'
Copy-Item -LiteralPath (Join-Path $root 'native\IconFlow.ShellExtension\appxmanifest.xml') -Destination $manifest -Force
Copy-Item -LiteralPath (Join-Path $sparseDir 'Assets') -Destination $OutputDirectory -Recurse -Force
$pfx = Join-Path $sparseDir 'IconFlow.DevCertificate.pfx'
$certificatePassword = ([guid]::NewGuid().ToString('N') + 'aA1!')
winapp cert generate --manifest $manifest --output $pfx --password $certificatePassword --valid-days 1095 --export-cer
if ($LASTEXITCODE -ne 0) { throw 'Development certificate generation failed.' }
Copy-Item -LiteralPath ([IO.Path]::ChangeExtension($pfx, '.cer')) -Destination (Join-Path $OutputDirectory 'IconFlow.DevCertificate.cer') -Force
winapp package $manifest --output (Join-Path $OutputDirectory 'IconFlow.ContextMenu.msix') --cert $pfx --cert-password $certificatePassword
if ($LASTEXITCODE -ne 0) { throw 'Windows 11 context-menu identity package generation failed.' }
winapp embed-identity $packageHost --manifest $manifest
if ($LASTEXITCODE -ne 0) { throw 'Application identity embedding failed.' }
winapp sign $packageHost $pfx --password $certificatePassword
if ($LASTEXITCODE -ne 0) { throw 'Package host signing failed.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Install-Win11Menu.ps1') -Destination $OutputDirectory -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Uninstall-Win11Menu.ps1') -Destination $OutputDirectory -Force

$required = @('IconFlow.exe', 'IconFlow.PackageHost.exe', 'App.xbf', 'MainWindow.xbf', 'QuickChangeWindow.xbf', 'IconEditorWindow.xbf', 'IconFlow.pri', 'IconFlow.ShellExtension.dll', 'IconFlow.ContextMenu.msix', 'Install-Win11Menu.ps1')
foreach ($name in $required) {
    $path = Join-Path $OutputDirectory $name
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing release resource: $name" }
}

$builtInAssets = Get-ChildItem -LiteralPath (Join-Path $OutputDirectory 'Assets\BuiltinIcons') -File -ErrorAction Stop
if (($builtInAssets | Where-Object Extension -eq '.png').Count -ne 9 -or
    ($builtInAssets | Where-Object Extension -eq '.ico').Count -ne 9) {
    throw 'The release must contain nine PNG previews and nine multi-size ICO files.'
}

$files = Get-ChildItem -LiteralPath $OutputDirectory -File -Recurse
$sizeMb = [math]::Round(($files | Measure-Object Length -Sum).Sum / 1MB, 1)
Write-Host "PASS: native build, core regression tests, and WinUI release validation completed."
Write-Host "Output: $OutputDirectory"
Write-Host "Files: $($files.Count); size: $sizeMb MB"
