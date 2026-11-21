# 发布与维护

## 小红书群的便捷取用方式

普通群友只展示两个轻量成品：Windows独立桌面版，以及已有Codex / ChatGPT桌面应用用户的原生版。若群聊实际支持ZIP附件就直接上传；否则将两份ZIP放在自己的网盘文件夹，群里置顶同一个文件夹链接与版本选择说明。不要假定所有群聊都支持ZIP。

源码、浏览器编辑器、皮肤模板和宣传素材单独放「进阶资料」。群公告注明Windows电脑使用，轻点／双击／悬停快捷按钮属于独立版；原生交互由宿主应用控制。

## 后续公开维护的下载入口

把以下三个 ZIP 作为同一版本的 Release 附件：

- Native：给已经使用 Codex / ChatGPT 原生萌宠的人。
- Portable：给只想下载运行、点动作和使用小工具的人。
- Source：给想修改程序、制作自己角色的人。

另附 `SHA256SUMS.txt` 和更新说明。GitHub 自动生成的 Source code ZIP 是仓库快照，**不等于已编译好的 Portable ZIP**。[GitHub Releases 文档](https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases)

先运行构建和测试，再打包：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\Test.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-Installer.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Package-Release.ps1
```

默认 ZIP 写入 `releases`；可以用 `-OutputDirectory <目录>` 指定输出。打包脚本只选择项目文件，不打包用户设置、笔记、账号资料、诊断日志或制作过程的提示词。

给群友分享时，在打包命令末尾加 `-GroupShare`，会生成带中文说明的轻量 Windows 包、Codex 原生外观包、完整源码 ZIP 和 SHA256 校验文件。

创建仓库后，在 GitHub Releases 准备一个与 `VERSION` 一致的预发布版本，上传 ZIP、校验文件和预览。此项目没有预填虚构的仓库、下载地址或维护者账号，也没有自动向公开网站发帖。[GitHub 管理 Release](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository)

## 小红书展示

分享预览可使用 `media` 中的竖图、GIF 或视频，先展示真实动作，再给三步安装和动作入口。竖图按便于阅读的 3:4 设计，不把它写成平台唯一的上传标准。

文案应明确区分：

- 原生版跟随应用任务状态，左右拖动触发跑步。
- 独立版提供手动动作、配置编辑和本机工具。
- 离线编辑器是在浏览器里预览和导出配置，不是假装一个新的原生 Codex 功能。

建议将下载入口指向自己维护的 Release 页面，并在笔记里写明版本号与 Windows 支持范围。代码 MIT 与角色素材许可分别说明，不使用“所有素材随便商用”等超出项目授权范围的表述。[GitHub 许可说明](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/licensing-a-repository)

## 接收问题反馈

请用户提供应用版本、系统、操作步骤、预期和实际结果，以及局部截图。不要让用户上传整份 Codex 配置、账号信息或个人笔记。已知“预览正常但浮窗仍为默认机器人”的问题，先刷新，再完整重启桌面应用。

提交修改时分别说明：改了什么、为什么、跑过哪些检查；修改图集后附真实图集生成的动画预览，不能只看静态封面。
