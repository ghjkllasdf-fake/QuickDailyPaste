# QuickPaste

> 🇬🇧 [English](#english) | 🇨🇳 [中文](#中文)

---

## English

A lightweight Windows tray app that pops up a snippet menu at your cursor and auto-pastes the selection into the active input.

### Quick Start
1. Double-click `QuickPaste.exe` to run (lives in the system tray)
2. Press `Ctrl+Shift+Q` to pop up the snippet menu at your mouse cursor
3. Click an item — it's copied to the clipboard and auto-pasted into the active input
4. Right-click the tray icon → **Settings** to manage hotkeys, snippets, and auto-start

**Rebuild from source:** double-click `build.cmd` (uses Windows' built-in .NET Framework 4 compiler — no SDK install required)

### Files
| File | Description |
|------|-------------|
| `QuickPaste.exe` | Main executable (double-click to run) |
| `QuickPaste.cs` | Source code (single-file C# WinForms) |
| `build.cmd` | Build script (uses bundled `csc.exe`) |
| `snippets.json` | User snippet config (auto-generated on first run) |
| `app.ico` | Tray icon (optional — embedded in exe as fallback) |

### Features
- Global hotkey (default `Ctrl+Shift+Q`) pops up snippets at the cursor
- Click an item → auto-copy + auto-paste into the active input
- Tray-resident; menu auto-closes on Esc or click-outside
- Custom snippets via `snippets.json` (supports separators)
- Dedicated hidden hotkey host — closing settings won't unregister the hotkey
- Restores focus to the previously active window before pasting
- Auto-start toggle (manages a `.lnk` in `shell:startup`)
- Fluent dark settings UI (Win11 rounded corners + dark title bar)
  - Hotkey capture and rebind
  - Snippet add / edit / delete / reorder
  - Auto-start toggle

### Security Notice
- Snippets are stored in **plain text** in `snippets.json` and written to the system clipboard when used
- **Do not store passwords, API keys, credit card numbers, or other high-sensitivity data** — any process in the same session can read the clipboard
- Recommended use: emails, addresses, common code snippets, command-line arguments, and other non-sensitive text

### Distribution
Just share `QuickPaste.exe` — that's all the recipient needs. Works on Windows 7+ (no .NET install required since .NET Framework 4 is system-bundled).

### License
MIT — see [LICENSE](LICENSE)

---

## 中文

一个轻量的 Windows 托盘小工具：按下热键在鼠标位置弹出预设短语菜单，点击即可自动粘贴到当前输入框。

### 快速开始
1. 双击 `QuickPaste.exe` 运行（系统托盘常驻）
2. 按 `Ctrl+Shift+Q` 在鼠标位置弹出预设文本菜单
3. 点击菜单项自动粘贴到当前输入框
4. 托盘右键 → **设置**，管理快捷键、预设短语、开机自启

**从源码重新编译：** 双击 `build.cmd`（使用 Windows 自带的 .NET Framework 4 编译器，无需安装 SDK）

### 文件说明
| 文件 | 说明 |
|------|------|
| `QuickPaste.exe` | 主程序（双击运行） |
| `QuickPaste.cs` | 源代码（单文件 C# WinForms） |
| `build.cmd` | 编译脚本（使用系统自带的 `csc.exe`） |
| `snippets.json` | 用户配置（首次运行自动生成） |
| `app.ico` | 托盘图标（可选，已内嵌到 exe 中） |

### 功能
- 全局热键（默认 `Ctrl+Shift+Q`）在鼠标位置弹出预设文本菜单
- 点击菜单项自动复制 + 粘贴到当前输入框
- 系统托盘常驻，点击外部或 Esc 自动关闭菜单
- `snippets.json` 自定义预设内容（支持分隔线）
- 独立热键宿主窗口，设置窗口关闭不影响热键注册
- 粘贴前自动恢复之前的活动窗口焦点
- 开机自启动开关（自动管理 `shell:startup` 中的 `.lnk` 快捷方式）
- Fluent Design 暗色设置 UI（Win11 圆角 + 暗色标题栏）
  - 热键捕获与修改
  - 预设短语增删改 + 排序
  - 开机自启动开关

### 安全提示
- 预设短语会以**明文**保存在 `snippets.json` 中，使用时会写入系统剪贴板
- **不要在此处存储密码、密钥、信用卡号等高敏感信息** —— 任何同会话的进程都可能读取剪贴板
- 推荐用途：邮箱、地址、常用代码片段、命令行参数等非敏感文本

### 分发
直接把 `QuickPaste.exe` 发给对方即可，无需其他文件。Windows 7+ 系统自带 .NET Framework 4，开箱即用。

### 开源协议
MIT —— 详见 [LICENSE](LICENSE)
# QuickPaste - 全局快速粘贴工具

## 快速开始
1. 双击 `QuickPaste.exe` 运行（系统托盘常驻）
2. 按 `Ctrl+Shift+Q` 在鼠标位置弹出预设文本菜单
3. 点击菜单项自动粘贴到当前输入框
4. 托盘右键 → 设置，管理快捷键、预设短语、开机自启

**重新编译：** 双击 `build.cmd`（需要 Windows 内置 .NET Framework 4）

## 文件说明
| 文件 | 说明 |
|------|------|
| `QuickPaste.exe` | 主程序（双击运行） |
| `QuickPaste.cs` | 源代码（单文件 C# WinForms） |
| `build.cmd` | 编译脚本（使用系统自带 csc.exe） |
| `snippets.json` | 预设文本配置 |

## 功能

- 全局热键 `Ctrl+Shift+Q` 在鼠标位置弹出预设文本菜单
- 点击菜单项自动复制 + 粘贴到当前输入框
- 系统托盘常驻，点击外部或 Esc 自动关闭菜单
- `snippets.json` 自定义预设内容（支持分隔线）
- 独立热键宿主窗口，设置窗口关闭不影响热键
- 粘贴前自动恢复目标窗口焦点
- 开机自启动开关（自动管理 `shell:startup` 快捷方式）
- Fluent Design 暗色设置 UI（Win11 圆角 + 暗色标题栏）
  - 热键捕获与修改
  - 预设短语增删改 + 排序
  - 开机自启动开关

## 安全提示
- 预设短语会以**明文**保存在 `snippets.json` 中，复制时会写入系统剪贴板
- **不要在此处存储密码、密钥、信用卡号等高敏感信息** —— 任何同会话进程都可能读取剪贴板
- 推荐用途：邮箱、地址、常用代码片段、命令行参数等非敏感文本
