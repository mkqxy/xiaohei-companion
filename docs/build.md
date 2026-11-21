# 本地构建与检查

需要 Windows 10/11、Windows PowerShell 5.1 或 PowerShell 7，以及系统 .NET Framework 的 C# 编译器和 WPF 程序集。常见 Windows 10/11 版本已包含 .NET Framework；本脚本不安装或下载运行时。[Microsoft 安装说明](https://learn.microsoft.com/en-us/dotnet/framework/install/on-windows-10)

在项目根目录运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\Test.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-Installer.ps1
```

`ExecutionPolicy Bypass` 只用于这次进程调用；脚本不修改系统的执行策略。先阅读下载的脚本，确认来源后再运行。

产物是 `dist/XiaoheiCompanion.exe`。`Build.ps1 -OutputDirectory <目录>` 可指定其他产物目录。默认 PNG 图集作为 `Xiaohei.DefaultAtlas.png` 嵌入；外置编辑器、配置示例和素材也会复制到输出目录。

构建直接调用 .NET Framework `csc.exe`，以 C# 5 编译全部 `src/*.cs`，显式引用 WPF 和标准库。没有 NuGet、Node 或 Python 依赖。[Microsoft 编译器选项](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/) · [嵌入资源选项](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/resources)

`Test.ps1` 编译并执行计时、陪伴状态、图集播放和 WPF 界面检查。每次运行使用 `build/tests/<唯一编号>` 中的隔离数据目录，不写入正常用户的笔记和设置。WPF 界面检查需要 Windows 图形会话。

`scripts/Test-Installer.ps1` 只向项目 `build` 内的临时 Codex 目录安装，覆盖首次安装、重复安装、更新备份、非法文件和提交失败回滚，不修改真正的萌宠目录。

`.github/workflows/build.yml` 为手动触发、推送和拉取请求准备了 Windows 构建及检查流程。文件存在不表示仓库已经创建或在线 CI 已通过；以实际运行结果为准。
