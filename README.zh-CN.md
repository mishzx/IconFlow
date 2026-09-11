# IconFlow Native

![IconFlow WinUI 3 演示](docs/media/iconflow-demo.gif)

IconFlow 是一个本地优先、可恢复的 Windows 图标管理器。它把更换图标、整理图标库和回滚历史放在同一个轻量界面中，让文件夹和快捷方式拥有清晰、可维护的视觉语义。

IconFlow 的意义不只是“换一个图标”：文件夹图标是个人知识库、研究资料和日常工作流的第一层导航。把图标、分类、收藏和历史管理放到本机，可以减少视觉噪音、提高定位效率，同时让文件名、路径和导入素材留在用户自己的设备上。项目采用 MIT 许可证，欢迎审阅、改进和本地化。

> 当前原生主线版本：`0.8.1`。公开仓库仅包含维护中的原生主线。

## 语言

- [English](README.md)
- [简体中文](README.zh-CN.md)
- [日本語](README.ja.md)
- [Español](README.es.md)

## 为什么选择 IconFlow

- **轻量原生界面**：基于 .NET 8、Windows App SDK / WinUI 3，使用 Windows 原生控件和 Mica 视觉，不需要启动 Chromium。
- **本地优先**：图标导入、预览、编辑、备份、历史和搜索均在本机完成；默认不联网、不上传文件，也不要求账号。
- **可恢复**：应用图标前自动备份；历史记录可以查看前后图标、打开对象位置并逐条撤销，也可以恢复默认图标。
- **贴近 Explorer**：支持 Windows 11 一级右键菜单，并提供无需管理员权限的“显示更多选项”兼容入口。
- **面向真实工作流**：图标库支持文件夹、搜索、收藏、重复检测、最近使用和拖动整理，而不是一次性修改工具。

## 主要功能

- 为普通文件夹和 `.lnk` 快捷方式更换图标。
- 导入 PNG、JPG/JPEG、WEBP、SVG、BMP、ICO，并提取 EXE 图标。
- 自动生成 16、24、32、48、64、128、256 七种尺寸的 ICO。
- 图标库：搜索、收藏、重复检测、最近使用、文件夹分类、新建/重命名/拖动整理。
- 内置 Fluent 文件夹图标包，覆盖文献、数据、代码、实验、图片、临床、待办、已完成和归档等场景。
- 原生编辑器：自由或固定比例裁剪、缩放、偏移、透明边距、圆角、底层图形和颜色。
- 可按背景色与容差移除边缘连通背景，同时保留内部白色符号。
- 右键轻量窗口：靠近鼠标显示、自动避让屏幕边缘、实时搜索、拖入图片并直接应用。
- 图标右键菜单：重命名、编辑、移动、收藏和安全删除。
- 版本化 256×256 预览缓存，优先读取 ICO 中最大的 PNG 图层，降低旧缓存或小图层放大造成的模糊。
- 历史记录、撤销、恢复默认，以及资源管理器原生刷新。
- 浅色、深色和跟随系统主题；支持 100%–200% DPI 自适应。
- 内置多语言资源：简体中文、繁体中文、英语、西班牙语、法语、德语、葡萄牙语、日语、韩语、俄语和阿拉伯语；阿拉伯语支持 RTL 布局。

## 隐私与数据

IconFlow 的设计目标是“文件留在本机”。默认行为如下：

- 不开机启动、不驻留托盘；关闭最后一个窗口后进程退出。
- 不上传文件名、路径、桌面内容或导入图片；默认不联网。
- 图标应用前会创建备份，已应用图标会复制到稳定的 `managed-icons` 目录，因此移动原始素材或图标库后不会立即失效。
- 应用数据默认位于 `%LocalAppData%\IconFlow`。请勿把包含个人文件名、路径或图标的诊断压缩包公开到 issue；提交问题时优先使用脱敏截图和可复现步骤。

## 安装与使用

### 直接运行便携版

1. 从 GitHub Releases 下载最新的 `IconFlow-0.8.1-win-x64-FINAL.zip`。
2. 将压缩包解压到你有写入权限的目录。
3. 双击 `IconFlow.exe` 启动。

发布包是面向 Windows x64 的原生构建，不包含用户数据。首次运行前请确认系统已安装 **.NET 8 Desktop Runtime** 和 **Windows App Runtime 2.4**；Windows 11 用户可以使用一级右键菜单功能。

### 右键菜单

- **Windows 11 一级菜单**：在发布目录运行 `Install-Win11Menu.ps1`，或在设置中选择“安装 / 修复 Win 11 新版菜单”。该操作会请求一次 UAC，并安装本地签名的 sparse identity 包。
- **兼容入口**：在设置中启用“资源管理器兼容右键菜单”。它只写入当前用户注册表，无需管理员权限，会出现在 Windows 11 的“显示更多选项”中。
- **卸载一级菜单**：运行 `Uninstall-Win11Menu.ps1`。它会移除菜单包和对应开发签名证书。

在公开分发时，开发签名证书仅用于验证和测试；正式产品应替换为受信任的代码签名证书或 Microsoft Store 分发。

## 从源码构建

需要 Windows、.NET 8 SDK、Windows App SDK 依赖和 PowerShell：

```powershell
dotnet build native\IconFlow.WinUI\IconFlow.WinUI.csproj -c Release
dotnet run --project native\IconFlow.Native.Tests\IconFlow.Native.Tests.csproj -c Release
powershell -ExecutionPolicy Bypass -File scripts\build-native.ps1
```

`native\\IconFlow.Core` 保存与界面无关的 Windows 核心逻辑，`native\\IconFlow.WinUI` 负责原生界面。构建脚本会生成内置图标、运行核心回归测试、发布 WinUI 文件并准备 Windows 11 菜单扩展。


## 当前边界

- 目前面向 Windows x64；系统桌面图标、文件类型图标、任务栏、开始菜单和批量规则尚未纳入可用核心版本。
- 一级右键菜单的开发构建使用本地开发证书，会显示 Windows 信任提示；公开发布前应重新签名。
- 发布包依赖本机 .NET 8 Desktop Runtime 和 Windows App Runtime 2.4；运行环境差异可能影响安装和菜单扩展。
- IconFlow 不会自动上传崩溃日志。报告问题时请主动移除用户名、组织名、个人路径和私有图标素材。

## 许可证

MIT License。第三方运行时和 Windows App SDK 组件遵循各自许可证。
