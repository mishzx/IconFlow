# Contributing to IconFlow / 参与 IconFlow

Thank you for helping make local Windows icon management safer and more
useful. Contributions and issue reports are welcome in English, 简体中文,
繁體中文, Español, Français, Deutsch, Português, 日本語, 한국어, Русский, or
العربية. Please keep user privacy, accessibility, and honest release claims
in mind.

感谢你帮助把 Windows 本地图标管理做得更安全、更实用。欢迎使用 English、简体中文、繁體中文、Español、Français、Deutsch、Português、日本語、한국어、Русский 或 العربية 提交贡献和 issue。请始终关注用户隐私、可访问性，并对发布能力作出准确说明。

## Before you start / 开始之前

- Search existing issues and pull requests before opening a new one.
- For a security vulnerability, follow [SECURITY.md](SECURITY.md) instead of
  opening a public issue with exploit details.
- Keep raw user assets, local test data, screenshots containing names or paths,
  and generated release directories outside the change you submit.
- Do not commit passwords, tokens, private keys, `.pfx`, `.p12`, `.key`, `.pem`,
  or local development certificates.

- 提交前请先搜索已有 issue 和拉取请求。
- 安全漏洞请遵循 [SECURITY.md](SECURITY.md)，不要在公开 issue 中披露利用细节。
- 原始用户资产、本地测试数据、含姓名或路径的截图和生成的发布目录应留在提交范围之外。
- 不要提交密码、令牌、私钥、`.pfx`、`.p12`、`.key`、`.pem` 或本地开发证书。

## Native development / 原生开发

The maintained application is the Windows-native project under
`native/IconFlow.WinUI`. It targets .NET 8, WinUI 3, Windows App SDK, and
`win-x64`. Retired prototype sources are not part of the public repository.

当前维护的应用是 `native/IconFlow.WinUI` 下的 Windows 原生项目，目标为 .NET 8、WinUI 3、Windows App SDK 和 `win-x64`；已退役的原型源码不进入公开仓库。

Recommended prerequisites:

- Windows 10 version 2004 (build 19041) or newer; Windows 11 is recommended;
- .NET 8 SDK;
- network access to restore the NuGet packages declared by the projects;
- a Windows App SDK-compatible Windows SDK (the GitHub runner provides this).

推荐环境：Windows 10 2004（内部版本 19041）或更新版本，推荐 Windows 11；.NET 8 SDK；可恢复项目 NuGet 依赖的网络环境；以及兼容 Windows App SDK 的 Windows SDK（GitHub runner 已提供）。

From PowerShell at the repository root:

```powershell
dotnet restore native\IconFlow.WinUI\IconFlow.WinUI.csproj --runtime win-x64
dotnet build native\IconFlow.WinUI\IconFlow.WinUI.csproj -c Release --runtime win-x64 --no-restore
dotnet run --project native\IconFlow.Native.Tests\IconFlow.Native.Tests.csproj -c Release --runtime win-x64
```

The public CI workflow runs the same source build and native core regression
suite on a Windows runner. It intentionally does not call the local packaging
script, create a development signing certificate, or upload build output.

公开 CI 工作流会在 Windows runner 上执行同样的源码构建和原生核心回归测试。它不会调用本地打包脚本、创建开发签名证书或上传构建产物。

The optional `scripts\build-native.ps1` script is for local release assembly.
It creates a throwaway development certificate for local Windows 11 menu
testing; keep that certificate outside Git and remove it after testing. A
production release requires an approved signing process managed outside this
repository.

可选脚本 `scripts\build-native.ps1` 用于本地组装发布包。它会为本地 Windows 11 菜单测试创建临时开发证书；请将证书保存在 Git 之外，并在测试后删除。正式发布必须使用仓库之外、经过批准的签名流程。

## Localization / 国际化

Keep user-facing strings in the matching
`native/IconFlow.WinUI/Strings/<locale>/Resources.resw` resource files. Preserve
resource keys across locales, keep accelerator/format placeholders intact,
and check right-to-left layout when changing Arabic resources. At minimum,
test the default English resource and the locale you changed. New languages
should include a short note about fallback behavior and text expansion.

请将界面文案放入对应的 `native/IconFlow.WinUI/Strings/<locale>/Resources.resw` 资源文件。各语言之间请保持资源键一致，保留快捷键和格式化占位符，并在修改阿拉伯语资源时检查从右到左布局。至少测试默认英语资源和你修改的语言；新增语言时请说明回退行为和文本长度变化。

## Pull requests / 拉取请求

Keep changes focused and explain the user-visible reason in the PR body. A
useful PR should include:

- a short summary and the affected area;
- commands used to validate the change and their result;
- screenshots or recordings only after removing personal paths and files;
- localization notes for changed or added strings;
- any compatibility, privacy, or performance trade-offs.

请保持每个 PR 的范围清晰，并在描述中说明面向用户的原因。建议包含：变更摘要和影响范围、实际运行的验证命令及结果、删除个人路径和文件后的截图或录屏、国际化文案说明，以及兼容性、隐私或性能权衡。

Before requesting review, confirm:

- [ ] Native build succeeds on Windows.
- [ ] The native regression suite passes.
- [ ] No secrets, private certificate material, local user data, or generated
      release directory is included.
- [ ] User-facing strings are localized or have an intentional fallback.
- [ ] The change does not claim cloud upload, telemetry, or support that it
      does not actually implement.

发起 review 前请确认：Windows 原生构建成功；原生回归测试通过；没有提交密钥、私有证书、本地用户数据或生成的发布目录；用户可见文案已本地化或有明确回退；描述没有夸大实际未实现的云上传、遥测或平台支持。
