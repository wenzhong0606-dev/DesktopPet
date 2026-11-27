# M1 图片桌宠 V1.0 — 仓库基线与验收清单

- 日期：2026-11-27
- 分支：main
- 检查的 HEAD：`4b48de096f81c39f3c29cbb3816c5b19eb9cc271`
- 当前工程：.NET 10 WPF
- 本文档是代码仓库静态检查结果；不代表已在 Windows 本机重新编译或运行。

## 1. 当前仓库结构

- `BichonDesktopPet.sln`
- `src/BichonDesktopPet.App`：WPF 程序、主窗口和桌面服务。
- `src/BichonDesktopPet.Core`：设置和基础模型。
- `tests/BichonDesktopPet.Tests`：MSTest。
- `assets/characters/bichon`：比熊资源。
- `docs`：产品、架构、视觉和资源文档。

## 2. 已存在功能（从 main 代码确认）

| 功能 | 当前实现 |
|---|---|
| .NET 10 WPF | 已存在 |
| 280 × 360 DIP 透明无边框窗口 | 已存在 |
| PNG 图片加载 | 已存在 |
| 图片缺失/解码失败时矢量备用显示 | 已存在 |
| 鼠标拖动 | 已存在 |
| 位置保存和恢复 | 关闭/隐藏时保存，启动时恢复并限制在主工作区 |
| 单击反馈 | 已存在 |
| 双击礼物反馈 | 已存在 |
| 雪花效果 | 已存在，最多 26 个粒子 |
| 装饰灯效果 | 已存在 |
| 系统托盘 | 已存在 |
| 托盘显示入口 | 已存在 |
| 托盘雪花/灯光/音乐/退出菜单 | 已存在 |
| 本地 JSON 设置 | 已存在，异常时回退默认值 |
| 可选背景音乐 | 代码已存在；需要 `assets/audio/holiday-ambient.mp3` 才可播放 |
| 置顶默认值 | 已存在；主窗口从设置读取 |
| 自动化测试 | 当前有 3 个设置默认值测试 |

## 3. 当前待补齐或需验证

1. **明确隐藏入口**：当前窗口关闭事件会隐藏窗口，但托盘菜单没有独立“隐藏”命令。
2. **设置 UI**：当前有设置模型和 JSON 服务，但没有独立设置窗口；置顶等设置缺少完整的用户操作入口。
3. **异常诊断**：设置读写使用静默 catch；缺少可供排查的本地日志。
4. **资产完整性**：PNG 文件已存在于 `assets/characters/bichon/base/bichon-idle.png`，Git tree 中记录大小为 132,158 bytes；早期文档预期 45,559 bytes、SHA-256 为 `049ab8ebe317a4934d60546da4646076053a59af689d3bca6313a6cf3dbf3b3c`。两者不一致，必须重新计算本地文件哈希并核对尺寸，不能沿用旧哈希。
5. **Windows 运行验证**：本轮仅检查了远程代码，未在用户 Windows 设备上运行程序。
6. **位置与多屏**：当前使用 `SystemParameters.WorkArea` 恢复窗口，需在实际多显示器/DPI 场景测试。
7. **托盘图标**：当前使用通用 `SystemIcons.Application`，可在后续 UI 完成后再替换为正式图标。

## 4. M1 任务顺序

### M1-01：确认代码与资产基线
- 本机拉取 main。
- 检查 `bichon-idle.png` 实际尺寸和 SHA-256。
- 执行 Release Build 和现有测试。
- 在 Windows 启动并确认显示的是 PNG，而不是矢量 fallback。

### M1-02：托盘显示/隐藏
- 托盘菜单提供明确的“显示比熊”和“隐藏比熊”操作。
- 隐藏时进程保持运行。
- 托盘双击恢复显示。
- 退出必须真正释放定时器、音频和 NotifyIcon。

### M1-03：轻量设置窗口
- 置顶开关。
- 雪花开关。
- 装饰灯开关。
- 音乐开关（无音乐素材时显示不可用状态）。
- 恢复默认位置。
- 变更立即生效并保存到现有 LocalAppData JSON。
- 不增加账号、云同步或复杂配置系统。

### M1-04：错误诊断
- 设置 JSON 损坏时不阻塞启动。
- 对设置保存失败给出可诊断记录。
- 记录未处理 UI 异常；对可恢复异常进行安全提示。
- 不记录用户对话或敏感信息。

### M1-05：测试与文档
- 增加设置序列化/反序列化测试。
- 增加损坏设置回退测试（必要时将设置路径注入以便测试）。
- 更新 README、资源清单、变更日志和 M1 验收记录。

## 5. M1 验收标准

### 构建
- [ ] `dotnet restore BichonDesktopPet.sln` 成功。
- [ ] `dotnet build BichonDesktopPet.sln -c Release` 成功。
- [ ] `dotnet test BichonDesktopPet.sln -c Release` 全部通过。

### Windows 功能
- [ ] 启动后加载真实 PNG 图片。
- [ ] 图片透明背景正确，无矢量 fallback 意外覆盖。
- [ ] 桌宠可拖动。
- [ ] 关闭/隐藏窗口不会终止进程。
- [ ] 托盘显示/隐藏和双击恢复正常。
- [ ] 退出菜单可完整退出并释放资源。
- [ ] 置顶开关即时生效且重启后保持。
- [ ] 雪花、灯光开关即时生效且重启后保持。
- [ ] 音乐关闭时不播放；无授权素材时明确提示不可用。
- [ ] 单击和双击互动正常。
- [ ] 首次启动位置合理；保存位置可恢复。
- [ ] 主显示器和至少一个多屏/DPI 场景没有窗口丢失。
- [ ] 断网情况下基础桌宠正常运行。

### 回归
- [ ] 不改变 280 × 360 DIP 基础尺寸，除非用户另行确认。
- [ ] 不引入 AI、语音、3D、用户模型导入或多角色功能。
- [ ] 已有图片显示、雪花、灯光、礼物互动不回归。

## 6. 运行命令

在 Windows PowerShell、仓库根目录执行：

```powershell
dotnet restore BichonDesktopPet.sln
dotnet build BichonDesktopPet.sln -c Release
dotnet test BichonDesktopPet.sln -c Release
dotnet run --project src/BichonDesktopPet.App/BichonDesktopPet.App.csproj
```

## 7. 退出条件

M1 所有构建、测试和 Windows 冒烟测试通过，且 README、资源清单和版本记录准确后，才标记为 M1 完成并进入 M2。
