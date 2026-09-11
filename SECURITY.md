# Security / 安全

IconFlow is designed as a local-first Windows application. It is intended to
work without uploading icon files or personal data. We welcome responsible
reports that help keep the application, its build process, and its release
documentation safe.

IconFlow 是一款本地优先的 Windows 应用，设计目标是在不上传图标文件或个人数据的情况下工作。我们欢迎负责任地报告应用、构建流程和发布文档中的安全问题。

## Reporting a vulnerability / 报告漏洞

Please use GitHub's private **Security Advisories** flow (the **Security** tab
on this repository, then **Report a vulnerability**) whenever it is available.
This keeps exploit details and any attached evidence out of public issues. If
that option is not available, open a minimal public issue asking the
maintainer to enable a private channel; do not include reproduction details,
credentials, personal files, or exploit code in that issue.

如果仓库已启用 GitHub **Security Advisories**，请使用仓库 **Security** 页面中的 **Report a vulnerability** 私密流程。这样可以避免漏洞细节和证据出现在公开 issue 中。如果该入口不可用，请只提交一个请求维护者开启私密沟通渠道的简短公开 issue；不要在其中附上复现细节、凭据、个人文件或漏洞利用代码。

Please include, in a private report:

- the affected version or commit, and Windows version;
- a concise description of the security impact;
- minimal reproduction steps or a proof of concept, if safe to share;
- whether the issue is already public or actively exploited;
- a safe way to contact you, if a reply is needed.

私密报告建议包含：受影响的版本或提交、Windows 版本、安全影响的简短说明、必要且安全的最小复现步骤、是否已经公开或正在被利用，以及（如需回复）安全的联系方式。

Please do not send passwords, access tokens, private signing keys, `.pfx`,
`.p12`, `.key`, or other private certificate material. Remove usernames,
absolute paths, personal icon files, and unrelated logs from attachments.

请勿发送密码、访问令牌、私有签名密钥、`.pfx`、`.p12`、`.key` 或其他私有证书材料。附件中请删除用户名、绝对路径、个人图标文件和无关日志。

## Release and build security / 发布与构建安全

- GitHub Actions builds and tests source only. The public workflow does not
  package, sign, or upload a release.
- Signing certificates and private keys must stay in a local or dedicated
  release environment and must never be committed to this repository or
  pasted into an issue, pull request, or log.
- Do not enable a workflow that prints secrets, exports signing keys, or
  uploads a build directory containing certificates.
- Review generated archives and logs before publishing them. Keep user data
  under `%LocalAppData%\IconFlow` out of commits and release assets.

- GitHub Actions 只构建和测试源码，公开工作流不会打包、签名或上传发布包。
- 签名证书和私钥必须保存在本地或专用发布环境，绝不能提交到本仓库或粘贴到 issue、拉取请求和日志中。
- 不要启用会打印密钥、导出签名私钥，或上传包含证书的构建目录的工作流。
- 发布前请检查生成的压缩包和日志；不要将 `%LocalAppData%\IconFlow` 下的用户数据提交或作为发布资产上传。

## Supported versions / 支持范围

Please report security issues against the latest published version first. Older
versions may not receive fixes separately; update to the latest release before
sharing non-sensitive diagnostic information.

请优先针对最新发布版本报告安全问题。旧版本不一定会单独修复；在提供非敏感诊断信息前，请先更新到最新版本。
