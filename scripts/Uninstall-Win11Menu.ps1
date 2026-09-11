[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    $arguments = '-NoProfile -ExecutionPolicy Bypass -File "{0}"' -f $MyInvocation.MyCommand.Path
    $process = Start-Process powershell.exe -Verb RunAs -ArgumentList $arguments -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "新版菜单卸载未完成（退出码 $($process.ExitCode)）。" }
    exit 0
}
Get-AppxPackage -Name IconFlow -ErrorAction SilentlyContinue | Remove-AppxPackage
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
Write-Host 'PASS: Windows 11 新版右键菜单组件已移除。'
