<div align="center">
  <img src="assets/icon.png" width="96" alt="IconFlow 图标">
  <h1>IconFlow</h1>
  <p><strong>让 Windows 文件夹一眼可辨，也随时可恢复。</strong></p>
  <p>原生 WinUI 3 · 本地处理 · 可撤销</p>
  <p><a href="https://github.com/mishzx/IconFlow/releases/latest"><strong>下载 Windows 版</strong></a> · <a href="#三步完成">快速开始</a> · <a href="README.md">English</a></p>
</div>

<div align="center">
  <img src="docs/media/iconflow-quick-change.gif" width="470" alt="IconFlow 轻量窗口搜索、应用与撤销演示">
</div>

## 把一件小事真正做简单

更换 Windows 图标不该要求用户理解 ICO 图层、`desktop.ini`、快捷方式内部结构和资源管理器缓存。IconFlow 把这些步骤收进一个小巧的原生窗口：选择对象、选择图标、完成应用；原始状态会自动备份。

| 原生轻量 | 数据留在本机 | 恢复优先 |
|:--|:--|:--|
| .NET 8、Windows App SDK 与 WinUI 3，不启动 Chromium。 | 无账号、无遥测、无文件上传、无后台联网服务。 | 稳定保存已应用图标，记录历史，并提供撤销和恢复默认。 |

## 三步完成

1. 右键文件夹或快捷方式，选择“更换图标”。
2. 搜索图标库、拖入图片，或选择内置 Fluent 文件夹图标。
3. 点击图标即可应用；IconFlow 自动刷新资源管理器，并保留“撤销”。

轻量窗口会出现在鼠标附近，自动避开屏幕边缘，默认置顶，也可以直接拖入图片并自动选择。

## 主要能力

- 为普通文件夹和 `.lnk` 快捷方式更换图标。
- PNG、JPG/JPEG、WEBP、SVG、BMP、ICO 导入与 EXE 图标提取。
- 生成 16、24、32、48、64、128、256 七层 ICO，并使用 256 px 预览缓存。
- 自由/固定比例裁剪、缩放、边距、圆角、底层图形与边缘连通背景移除。
- 搜索、收藏、标签、重复检测，以及可拖动整理和重命名的图标文件夹。
- 历史按对象折叠，明确显示修改前后图标、打开位置与撤销操作。
- 浅色、深色、跟随系统主题，以及 100%–200% DPI 支持。
- 随附 11 种界面语言资源并支持阿拉伯语 RTL；欢迎协助校对翻译。

<details>
<summary><strong>查看完整界面演示</strong></summary>
<br>
<div align="center"><img src="docs/media/iconflow-demo.gif" width="900" alt="IconFlow 图标库与编辑器演示"></div>
</details>

## 下载与运行

从 [GitHub Releases](https://github.com/mishzx/IconFlow/releases/latest) 下载最新的 `win-x64-portable.zip`，解压到稳定且可写的目录，然后运行 `IconFlow.exe`。

运行要求：Windows 10 22H2 或 Windows 11 23H2+、x64、.NET 8 Desktop Runtime、Windows App Runtime 2.4。

IconFlow 默认不开机启动，也不驻留托盘。日后即使启用静默启动，也只执行轻量维护，不显示窗口，随后退出。

### Windows 11 右键菜单

可选的 Windows 11 一级菜单基于 `IExplorerCommand` sparse package。运行 `Install-Win11Menu.ps1`，或选择 **设置 → 安装 / 修复 Windows 11 菜单**；安装包及项目本地开发证书时会请求一次 UAC。无需提升权限的当前用户兼容菜单位于“显示更多选项”中。

安装开发签名菜单组件前，请阅读 [SECURITY.md](SECURITY.md) 和发布说明。便携应用本身不要求安装证书。

## 隐私与当前范围

应用数据默认保存在 `%LocalAppData%\IconFlow`，也可迁移图标库。软件不上传文件名、路径、桌面内容、软件列表、导入图片或历史记录，详见 [PRIVACY.md](PRIVACY.md)。

当前稳定核心支持文件夹和快捷方式；系统桌面图标、文件类型关联、任务栏、开始菜单内部项和批量匹配规则暂未纳入本版。

## 从源码构建

```powershell
dotnet build native\IconFlow.WinUI\IconFlow.WinUI.csproj -c Release
dotnet run --project native\IconFlow.Native.Tests\IconFlow.Native.Tests.csproj -c Release
powershell -ExecutionPolicy Bypass -File scripts\build-native.ps1
```

## 参与项目

欢迎提交缺陷报告、语言校对和聚焦改进。请先阅读 [CONTRIBUTING.md](CONTRIBUTING.md) 与 [SECURITY.md](SECURITY.md)，然后提交 Issue 或参与 [Discussions](https://github.com/mishzx/IconFlow/discussions)。

## 许可证

[MIT](LICENSE) © IconFlow contributors。
