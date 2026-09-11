[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$package = Join-Path $root 'IconFlow.ContextMenu.msix'
$certificate = Join-Path $root 'IconFlow.DevCertificate.cer'
if (-not (Test-Path -LiteralPath $package)) { throw '缺少 IconFlow.ContextMenu.msix。' }
if (-not (Test-Path -LiteralPath $certificate)) { throw '缺少 IconFlow.DevCertificate.cer。' }
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    $arguments = '-NoProfile -ExecutionPolicy Bypass -File "{0}"' -f $MyInvocation.MyCommand.Path
    $process = Start-Process powershell.exe -Verb RunAs -ArgumentList $arguments -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "新版菜单安装未完成（退出码 $($process.ExitCode)）。" }
    exit 0
}
$stores = @(
    'Cert:\CurrentUser\TrustedPeople',
    'Cert:\CurrentUser\TrustedPublisher',
    'Cert:\CurrentUser\Root',
    'Cert:\LocalMachine\TrustedPeople',
    'Cert:\LocalMachine\TrustedPublisher',
    'Cert:\LocalMachine\Root'
)
foreach ($store in $stores) {
    Get-ChildItem $store -ErrorAction SilentlyContinue |
        Where-Object { $_.Subject -eq 'CN=IconFlow' -and $_.Issuer -eq 'CN=IconFlow' } |
        Remove-Item -Force -ErrorAction SilentlyContinue
}
Import-Certificate -FilePath $certificate -CertStoreLocation 'Cert:\LocalMachine\TrustedPeople' | Out-Null
Import-Certificate -FilePath $certificate -CertStoreLocation 'Cert:\LocalMachine\TrustedPublisher' | Out-Null
Get-AppxPackage -Name IconFlow -ErrorAction SilentlyContinue | Remove-AppxPackage -ErrorAction SilentlyContinue
Add-AppxPackage -Path $package -ExternalLocation $root
Write-Host 'PASS: IconFlow 已进入 Windows 11 新版一级右键菜单。'
Write-Host '如资源管理器已打开，请重新打开该窗口以看到菜单。'
