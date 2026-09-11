[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$source = Join-Path $root 'native\IconFlow.ShellExtension\ShellExtension.cpp'
$definition = Join-Path $root 'native\IconFlow.ShellExtension\ShellExtension.def'
$devCmd = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\BuildTools\Common7\Tools\VsDevCmd.bat"
if (-not (Test-Path -LiteralPath $devCmd)) { throw '未找到 Visual Studio C++ Build Tools。' }
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$dll = Join-Path $OutputDirectory 'IconFlow.ShellExtension.dll'
$command = '"{0}" -arch=x64 -host_arch=x64 && cl.exe /nologo /utf-8 /std:c++20 /O2 /EHsc /DUNICODE /D_UNICODE /LD "{1}" /link /DEF:"{2}" /OUT:"{3}"' -f $devCmd,$source,$definition,$dll
cmd.exe /d /s /c $command
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $dll)) { throw 'Windows 11 右键菜单原生组件构建失败。' }
Write-Host "PASS: 原生 IExplorerCommand 已生成：$dll"
