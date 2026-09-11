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
if ($LASTEXITCODE -ne 0) { throw '内置图标生成失败。' }

dotnet clean $appProject -c $Configuration
if ($LASTEXITCODE -ne 0) { throw '清理失败。' }
dotnet build $appProject -c $Configuration
if ($LASTEXITCODE -ne 0) { throw 'WinUI 构建失败。' }
dotnet run --project $testProject -c $Configuration
if ($LASTEXITCODE -ne 0) { throw '核心回归测试失败。' }
dotnet publish $appProject -c $Configuration -r win-x64 --self-contained false `
    -p:WindowsAppSDKSelfContained=false -p:PublishSingleFile=false -o $OutputDirectory
if ($LASTEXITCODE -ne 0) { throw '发布失败。' }

& (Join-Path $PSScriptRoot 'build-shell-extension.ps1') -OutputDirectory $OutputDirectory
$packageHost = Join-Path $OutputDirectory 'IconFlow.PackageHost.exe'
Copy-Item -LiteralPath (Join-Path $OutputDirectory 'IconFlow.exe') -Destination $packageHost -Force
$sparseDir = Join-Path $env:TEMP ("IconFlow-sparse-" + [guid]::NewGuid().ToString('N'))
winapp init --exe $packageHost --sparse --output-dir $sparseDir --use-defaults --force
if ($LASTEXITCODE -ne 0) { throw 'Sparse identity 初始化失败。' }
$manifest = Join-Path $sparseDir 'appxmanifest.xml'
Copy-Item -LiteralPath (Join-Path $root 'native\IconFlow.ShellExtension\appxmanifest.xml') -Destination $manifest -Force
Copy-Item -LiteralPath (Join-Path $sparseDir 'Assets') -Destination $OutputDirectory -Recurse -Force
$pfx = Join-Path $sparseDir 'IconFlow.DevCertificate.pfx'
$certificatePassword = ([guid]::NewGuid().ToString('N') + 'aA1!')
winapp cert generate --manifest $manifest --output $pfx --password $certificatePassword --valid-days 1095 --export-cer
if ($LASTEXITCODE -ne 0) { throw '开发签名证书生成失败。' }
Copy-Item -LiteralPath ([IO.Path]::ChangeExtension($pfx, '.cer')) -Destination (Join-Path $OutputDirectory 'IconFlow.DevCertificate.cer') -Force
winapp package $manifest --output (Join-Path $OutputDirectory 'IconFlow.ContextMenu.msix') --cert $pfx --cert-password $certificatePassword
if ($LASTEXITCODE -ne 0) { throw 'Windows 11 新版菜单身份包生成失败。' }
winapp embed-identity $packageHost --manifest $manifest
if ($LASTEXITCODE -ne 0) { throw '应用身份嵌入失败。' }
winapp sign $packageHost $pfx --password $certificatePassword
if ($LASTEXITCODE -ne 0) { throw '应用身份宿主签名失败。' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Install-Win11Menu.ps1') -Destination $OutputDirectory -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Uninstall-Win11Menu.ps1') -Destination $OutputDirectory -Force

$required = @('IconFlow.exe', 'IconFlow.PackageHost.exe', 'App.xbf', 'MainWindow.xbf', 'QuickChangeWindow.xbf', 'IconEditorWindow.xbf', 'IconFlow.pri', 'IconFlow.ShellExtension.dll', 'IconFlow.ContextMenu.msix', 'Install-Win11Menu.ps1')
foreach ($name in $required) {
    $path = Join-Path $OutputDirectory $name
    if (-not (Test-Path -LiteralPath $path)) { throw "发布资源缺失：$name" }
}

$builtInAssets = Get-ChildItem -LiteralPath (Join-Path $OutputDirectory 'Assets\BuiltinIcons') -File -ErrorAction Stop
if (($builtInAssets | Where-Object Extension -eq '.png').Count -ne 9 -or
    ($builtInAssets | Where-Object Extension -eq '.ico').Count -ne 9) {
    throw '发布包必须包含 9 枚清晰 PNG 预览和 9 枚多尺寸 ICO。'
}

$files = Get-ChildItem -LiteralPath $OutputDirectory -File -Recurse
$sizeMb = [math]::Round(($files | Measure-Object Length -Sum).Sum / 1MB, 1)
Write-Host "PASS: 原生构建、核心回归和 WinUI 发布资源校验完成。"
Write-Host "输出：$OutputDirectory"
Write-Host "文件：$($files.Count)，大小：$sizeMb MB"
