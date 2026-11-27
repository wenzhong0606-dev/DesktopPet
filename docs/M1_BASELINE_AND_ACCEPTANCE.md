# M1 图片桌宠 V1.0 — 仓库基线与验收清单

- 日期：2026-11-27
- 分支：main
- 当前工程：.NET 10 WPF
- 用户已确认：上一轮图片桌宠基础功能及托盘显示/隐藏在本机 Windows 正常运行。
- M1 状态：用户已确认本机验收通过；设置窗口布局修复和 Windows CI 均已通过。

## 1. 当前仓库结构

- `BichonDesktopPet.sln`
- `src/BichonDesktopPet.App`：WPF 程序、主窗口、设置窗口和桌面服务。
- `src/BichonDesktopPet.Core`：设置模型和可测试的本地设置持久化服务。
- `tests/BichonDesktopPet.Tests`：MSTest。
- `assets/characters/bichon`：比熊资源。
- `docs`：产品、架构、视觉和资源文档。

## 2. 已有功能基线

用户已确认上一轮 M1 基础版本运行正常。仓库已有功能包括：

| 功能 | 状态 |
|---|---|
| .NET 10 WPF，280 × 360 DIP 透明窗口 | 已有；用户本机确认基础版本正常 |
| PNG 图片加载与矢量备用显示 | 已有 |
| 鼠标拖动、位置保存与恢复 | 已有 |
| 单击反馈、双击礼物互动 | 已有 |
| 雪花和装饰灯效果 | 已有 |
| 系统托盘显示、隐藏、退出 | 已有；用户本机确认基础版本正常 |
| 右键菜单隐藏入口 | 已有；用户本机确认基础版本正常 |
| 本地 JSON 设置 | 已有，现已移入 Core 服务并增加诊断回调 |
| 可选背景音乐 | 代码已存在；需要 `assets/audio/holiday-ambient.mp3` 才可播放 |

## 3. 图片资源完整性

- 路径：`assets/characters/bichon/base/bichon-idle.png`
- 尺寸：420 × 540
- 格式：8-bit RGBA
- 大小：132,158 bytes
- SHA-256：`326d10ace28d53e8a4289b9f103c648ecb79d6b1b32e4a8f76d715793b97d210`

## 4. 本轮新增实现

### M1-03：轻量设置窗口

- [x] 新增独立设置窗口。
- [x] 可从桌宠右键菜单打开。
- [x] 可从系统托盘菜单打开。
- [x] 置顶开关。
- [x] 雪花开关。
- [x] 装饰灯开关。
- [x] 背景音乐开关；未找到音频素材时禁用控件并显示说明。
- [x] 恢复桌宠默认位置。
- [x] 设置内容区支持纵向滚动，完成按钮固定在底部；窗口可调整大小。
- [x] 设置变更立即应用并保存到 LocalAppData JSON。
- [x] Windows 本机验证所有控件及重启持久化。

### M1-04：错误诊断

- [x] 设置 JSON 读取失败时回退默认设置并记录日志。
- [x] 设置 JSON 保存失败时记录日志，桌宠继续使用内存设置。
- [x] 记录 WPF UI Dispatcher、AppDomain、未观察 Task 异常。
- [x] 记录启动失败和比熊图片加载失败。
- [x] 日志位于 `%LOCALAPPDATA%\BichonDesktopPet\logs\app.log`。
- [x] 不记录对话或麦克风输入。
- [ ] Windows 本机确认：发生一条可控诊断事件后，日志文件在预期目录创建且内容可读。正常无异常运行时，日志文件可能尚不存在。

### M1-05：自动化测试与文档

- [x] 设置保存/读取 round-trip 测试。
- [x] 设置文件不存在时返回默认值测试。
- [x] JSON 损坏回退默认值并触发诊断回调测试。
- [x] 保存失败触发诊断回调测试。
- [x] GitHub Actions Windows CI Release Build 通过（0 warnings / 0 errors）。
- [x] GitHub Actions Windows CI 自动化测试通过（7/7）。
- [ ] 用户本机 Release Build/Test 复核。

CI 记录：[Windows CI run #65](https://github.com/wenzhong0606-dev/DesktopPet/actions/runs/45942365232)，设置窗口滚动修复 commit `baa37eeea53bb0bdbfa31c4b794b24193360f6c1`。Release 构建通过；原始设置服务测试记录见 [run #58](https://github.com/wenzhong0606-dev/DesktopPet/actions/runs/45934852652)。

## 5. 验收命令

在 Windows PowerShell、仓库根目录执行：

```powershell
git pull origin main
dotnet restore BichonDesktopPet.sln
dotnet build BichonDesktopPet.sln -c Release
dotnet test BichonDesktopPet.sln -c Release
dotnet run --project src/BichonDesktopPet.App/BichonDesktopPet.App.csproj
```

## 6. Windows 功能验收

### 已由用户确认的上一轮基础功能

- [x] 比熊图片正常显示。
- [x] 桌宠拖动和原有互动正常。
- [x] 托盘隐藏、恢复正常。
- [x] 退出操作正常。
- [x] 原有基础功能没有明显回归。

### 本轮新增功能待验证

- [ ] 从桌宠右键菜单打开设置窗口。
- [ ] 在较小窗口尺寸或较高 Windows 缩放比例下，滚动可访问全部设置，底部「完成」按钮保持可见。
- [ ] 从系统托盘菜单打开设置窗口。
- [ ] 置顶开关即时生效且重启后保持。
- [ ] 雪花开关即时生效且重启后保持。
- [ ] 装饰灯开关即时生效且重启后保持。
- [ ] 没有音频文件时音乐控件禁用并显示说明。
- [ ] 恢复默认位置后位置正确，重启后仍保持。
- [ ] 设置窗口关闭后桌宠仍正常运行。
- [ ] 发生诊断事件后，日志文件在预期目录生成；正常运行时日志文件可暂不存在。
- [ ] 断网情况下基础桌宠正常运行。

## 7. 日志与设置路径

- 设置：`%LOCALAPPDATA%\BichonDesktopPet\settings.json`
- 日志：`%LOCALAPPDATA%\BichonDesktopPet\logs\app.log`

日志记录应用诊断和异常堆栈；首次出现诊断事件时才创建文件。正常无异常运行时，日志文件可能尚不存在。日志不记录用户对话内容或麦克风输入。

## 8. M1 退出条件

- [x] Windows CI Release 构建通过。
- [x] 自动化测试通过。
- [x] 用户确认设置窗口、滚动布局和底部按钮本机验收通过。
- [x] 原有图片桌宠功能无回归。

**M1 已完成，正式进入 M2。**
