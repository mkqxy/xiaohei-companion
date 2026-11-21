# 从自己的工具调用独立版

本页只适用于 `XiaoheiCompanion.exe`。它不是 Codex 原生萌宠的控制 API，也不会改变原生应用的任务状态。

## 一条命令播放动作和台词

在解压后的 Portable 目录运行：

```powershell
.\XiaoheiCompanion.exe --action waving --message "今天也加油"
.\XiaoheiCompanion.exe --action running-right
.\XiaoheiCompanion.exe --action working --message "开始这一小段专注时间。"
```

支持 `idle`、`running-right`、`running-left`、`waving`、`jumping`、`failed`、`waiting`、`running`、`review`；`working` 是 `running` 的别名。

`--message` 可以省略，最多 160 字符。参数作为字面动作 ID 和文字处理，不会解释为脚本或终端命令。不要把外部输入拼成一整段终端代码；从其他语言调用时，使用该语言的进程参数数组 API。

程序已在运行时，请求交给同一个数据目录对应的实例，大约在下一次状态检查时播放，不会另开一个桌宠实例。程序未运行时，会启动并播放该动作。命令发送成功只代表请求已发送，不等于一个可供外部程序读取的动画完成回执。

返回码：成功启动或发送为 0；不支持的参数、动画或过长台词为 2；无法写入本机请求为 3。请求只承载动作和台词，过期两分钟的旧请求不会被重放。

## 另一个 PowerShell 脚本调用

```powershell
$pet = Join-Path $PSScriptRoot 'XiaoheiCompanion.exe'
& $pet --action review --message '检查一下，然后休息。'
if ($LASTEXITCODE -ne 0) { throw '没有成功发送小黑动作。' }
```

使用完整路径可以避免依赖当前目录。公开分享自己的脚本时，保留角色素材的说明；不要把自己的本机绝对路径或笔记目录写进脚本。

## 进一步修改代码

- `src/SpriteEngine.cs`：动作帧数、时间、视线索引和图集验证。
- `src/Customization.cs`：配置字段、范围和自定义按钮约束。
- `src/App.cs`：桌面交互、动作优先级、口袋界面和小工具。
- `src/CompanionModel.cs`：陪伴状态、喂食和互动冷却。
- `src/PetArt.cs`：图集绘制适配与备用绘图。

默认用户数据在 `%LOCALAPPDATA%\XiaoheiCompanionV3`。`XIAOHEI_DATA_DIR` 可用于隔离开发或自动化测试的数据；正常用户无需设置。使用不同数据目录会获得不同的程序实例，不会覆盖旧版笔记。

更改后运行根目录的构建和测试脚本，再用实际动画预览验证动作含义。不要用简单的整体缩放或旋转代替缺失的角色帧。
