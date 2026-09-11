# IconFlow platform launch kit

Public project identity: **IconFlow Contributors**  
Repository: https://github.com/mishzx/IconFlow  
Release: https://github.com/mishzx/IconFlow/releases/tag/v0.8.2

This file contains factual, privacy-safe copy for community submissions. It does
not request stars or votes and should always be adapted to each community's
current self-promotion rules.

## Product Hunt

**Tagline**

> Make Windows folders recognizable—and every change reversible

**Short description**

> IconFlow is a compact, native WinUI 3 icon manager for Windows. Change folder
> and shortcut icons, turn images into multi-size ICO files, organize a local
> library, and undo changes without uploading personal paths or files.

**Maker comment**

> I maintain IconFlow, an independent open-source project. Changing a Windows
> folder icon looks simple, but it often exposes ICO layers, `desktop.ini`,
> shortcut internals, and Explorer cache behavior. IconFlow turns that into a
> short visual workflow: choose an object, pick or import an icon, apply it, and
> keep an undo path. The v0.8.2 native preview includes a compact Explorer
> picker, image editing, seven-layer ICO generation, local library folders,
> grouped recovery history, and 11 interface locales. It is local-first: no
> account, telemetry, file upload, or background network service. Current scope
> focuses on folders and `.lnk` shortcuts. Feedback about Windows integration,
> packaging, accessibility, and translations is welcome through GitHub Issues
> and Discussions.

## AlternativeTo

**Short description**

> Native WinUI 3 icon manager for Windows folders and shortcuts. Import and edit
> images as ICO, organize them locally, and undo changes.

**Long description**

> IconFlow is a local-first Windows icon manager maintained by IconFlow
> Contributors. Change icons from a compact Explorer picker, drag in an image,
> generate 16–256 px ICO layers, crop and style the source, organize icons in
> local folders, and inspect or undo grouped history. Managed copies keep an
> applied icon available if the imported source is moved. IconFlow includes 11
> interface locales, light/dark/system themes, and an optional Windows 11
> first-level context-menu package. The v0.8.2 preview supports folders and
> `.lnk` shortcuts; system desktop icons, file-type associations, taskbar/Start
> internals, and bulk matching are outside the current stable scope. MIT
> licensed. Windows 10 22H2 or Windows 11 23H2+, x64, .NET 8 Desktop Runtime,
> and Windows App Runtime 2.4 are required.

## OSCHINA

**标题**

> IconFlow：基于 WinUI 3 的本地化 Windows 图标管理器

**简介**

> IconFlow 使用 .NET 8、Windows App SDK 与 WinUI 3，将文件夹和快捷方式的
> 图标更换、图片转 ICO、本地图标库与历史恢复收进一个小巧的原生窗口。

**正文**

> IconFlow 是由 IconFlow Contributors 维护的 MIT 开源项目，面向 Windows
> 10/11 x64。v0.8.2 支持轻量资源管理器选择器、图片拖入、PNG/JPG/WEBP/
> SVG/BMP/ICO 导入、EXE 图标提取、裁剪与样式编辑、16–256 px 多尺寸 ICO、
> 本地图标文件夹，以及按对象折叠的前后对比历史。软件不要求账号，不上传
> 文件名、路径、桌面内容、导入图片或历史记录，也没有后台联网服务。当前稳定
> 范围聚焦普通文件夹和 `.lnk` 快捷方式；系统桌面图标、文件类型关联、任务栏/
> 开始菜单内部项和批量匹配尚未包含。项目与下载：
> https://github.com/mishzx/IconFlow

Suggested tags: Windows, WinUI 3, .NET 8, Fluent Design, icon manager,
local-first, open source.

## Reddit self-promotion thread

Only use this in a community's designated self-promotion thread after checking
its current rules.

> I maintain IconFlow, an independent open-source Windows tool. It turns folder
> and `.lnk` icon changes into a compact, reversible workflow: select an object,
> choose or import an icon, apply it, and retain a local backup plus undo path.
> The v0.8.2 WinUI 3 preview includes drag-and-drop image import, multi-size ICO
> generation, crop/style tools, local library folders, grouped history, and 11
> interface locales. There is no account, telemetry, file upload, or background
> network service. Current scope is intentionally limited to folders and
> shortcuts. Repository: https://github.com/mishzx/IconFlow. Concrete feedback
> about Windows integration, packaging, localization, and recovery behavior is
> welcome.

## DEV article outline

Title: **Building a reversible Windows icon workflow with WinUI 3**

1. Why icon replacement touches `desktop.ini`, shortcuts, ICO frames, and cache.
2. Native architecture: `IconFlow.Core`, `IconFlow.WinUI`, and `IExplorerCommand`.
3. A compact picker that stays near the selected object without leaving screen.
4. Image import, connected-background removal, editing, and multi-size ICO output.
5. Recovery by design: managed copies, backups, grouped history, and safe cleanup.
6. Eleven resource locales, RTL behavior, and theme support.
7. Local privacy boundary, release hygiene, runtime requirements, and limitations.
8. Validation results and the next packaging/accessibility improvements.

DEV requires substantial technical content and disclosure for AI-assisted
articles. The final article should be reviewed and substantially authored by a
human maintainer rather than posted as promotional copy.

## Do not auto-submit

- **Hacker News:** publish only a personally written Show HN introduction and be
  available to answer technical questions.
- **V2EX:** its rules prohibit AI-generated content; write the post personally.
- Never request votes, coordinate stars, create duplicate accounts, or repost the
  same text across unrelated communities.
