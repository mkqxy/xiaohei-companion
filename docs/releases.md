# 固定下载入口与版本发布

给朋友分享一个固定的 GitHub 发行页。每次发布新版，页面展示最新版本；下载附件固定叫 `Xiaohei-Windows.zip`，以后不用在 PDF 或说明里逐次换版本链接。最新版页面与附件直链由 GitHub 官方支持。[GitHub 发行链接说明](https://docs.github.com/en/repositories/releasing-projects-on-github/linking-to-releases)

## 初次设置

1. 在自己的 GitHub 账号下建立公开仓库，上传本项目源码及 `.github` 目录。
2. 将下列示例中的 `OWNER/REPO` 换成自己的账号与仓库名，再把实际地址放到分享 PDF 或说明中。
3. 以后发布正式版时，保持 `Xiaohei-Windows.zip` 这个附件名。

```text
最新版说明页
https://github.com/OWNER/REPO/releases/latest

Windows 下载按钮
https://github.com/OWNER/REPO/releases/latest/download/Xiaohei-Windows.zip
```

上方是地址格式示例。实际仓库创建、首次发布完成后，这两个入口才会有可下载的内容。`latest` 用于正式发布；`3.8.0-preview.1` 这一类预发行版会使用自己的版本页和附件地址。[GitHub 最新正式版定义](https://docs.github.com/en/rest/releases/releases#get-the-latest-release)

## 以后怎样更新

把 `VERSION`、`src/AssemblyInfo.cs` 和界面版本文字改为新版本，提交到仓库。以正式版 `3.8.0` 为例，推送相应标签会启动 Windows 构建、检查、打包和发布。

```powershell
git tag v3.8.0
git push origin v3.8.0
```

也可以在 GitHub 的 **Actions → Publish Windows release → Run workflow** 里输入已经推送的版本标签。流程会核对标签与 `VERSION`，检查全部通过后，上传固定名称的桌面包、源码包和校验文件。发布使用运行当次的 GitHub 令牌；GitHub 托管运行器已预装 `gh`。仓库中的工作流会声明发行所需的 `contents: write` 权限。[GitHub CLI 工作流说明](https://docs.github.com/en/actions/how-tos/write-workflows/choose-what-workflows-do/use-github-cli)

流程会先上传成草稿，再公开发布。附件包括 Windows 桌面版、源码和可单独安装的原生外观 `Xiaohei-Codex-Pet.zip`。已有的同名版本会报错，避免覆盖历史发行。上传中断时，可以到 Releases 检查草稿，再决定补齐草稿或使用新版本号。[创建发行版命令](https://cli.github.com/manual/gh_release_create)

## 本机准备与上传

本机先构建、检查和打包。最后一条命令默认只在本地产出可上传的文件。

```powershell
.\Build.ps1
.\Test.ps1
.\scripts\Package-Release.ps1 -GroupShare
.\scripts\Publish-Release.ps1 -Repository OWNER/REPO -IncludeSource -IncludeNative
```

输出位于 `releases/publish/v版本号/`。`Xiaohei-Windows.zip` 与中文群分享包的内容和哈希一致，只是外层文件名固定了。需要使用自定义打包目录时，加 `-PackageDirectory 路径`。需要自己的发行说明时，加 `-NotesFile 路径`，文件采用 UTF-8。

要在本机上传，先安装 GitHub CLI 并使用 `gh auth login` 登录自己的账号；版本标签需要已经推送到仓库。明确带上 `-Publish` 的命令会上传并公开发布。

```powershell
.\scripts\Publish-Release.ps1 -Repository OWNER/REPO -IncludeSource -IncludeNative -Publish
```

## 其他入口

GitHub Pages 可以另做项目介绍和教程页面，普通公开仓库可使用 GitHub Free 的 Pages；下载按钮仍可指向上面的固定附件地址。当前代码包准备的是 Releases 流程。[GitHub Pages 说明](https://docs.github.com/en/pages/getting-started-with-github-pages/what-is-github-pages)

Gitee 也可以按版本发布软件附件。官方文档列出的普通项目限制是单个附件 100 MB、仓库附件总量 1 GB，当前几 MB 的桌面包在此范围内。若主要面向国内用户，可以先实际测试朋友们的下载体验，再决定是否同步一份 Gitee 发行页。此处未承诺任一服务在所有地区的连接速度。[Gitee 创建发行版](https://help.gitee.com/repository/release/create)

这个流程让大家从同一入口下载新版。已下载的桌宠可以退出后解压新版，继续使用本机的设置与随手记。
