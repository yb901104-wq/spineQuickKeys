## Context

虚拟按键窗口（VirtualKeyWindow）点击按钮执行 MacroSequence 时，SendKeys/keybd_event 模拟的键盘消息发到了 VK 窗口自身而非目标软件（Spine）。原因是 Windows 在按钮 Click 事件触发前已将焦点切换到 VK 窗口。

现有代码中 MacroPlayer 完全依赖 SendKeys/keybd_event，这些 API 天然向**当前前台窗口**发送消息。VirtualKeyWindow 的 `OnButtonClicked` 虽然尝试 `RestoreForeground(prevFg)`，但 `prevFg` 在 Click 时已是 VK 窗口句柄，恢复无效。

数据模型层面，MacroSequence 已有 `TargetAppPath` 字段，但目前仅用于 HotkeyService 的快捷键触发前检查，未在虚拟按键流程中使用。VirtualLayoutSerializer 存储窗口布局信息，可作为目标窗口信息的持久化位置。

## Goals / Non-Goals

**Goals:**
- 虚拟按钮点击后，按键消息能正确发送到目标软件（Spine）（P2）
- 提供用户友好的目标窗口捕获流程（右键菜单启动，自动捕获前台窗口）（P2）
- 目标窗口信息持久化到布局文件，跨会话保持（P2）
- 方案 A（PostMessage 后台注入）与方案 B（自动激活）共存，自动降级（P2）
- VkPickMode 在 SequenceEditor 中便捷绑定虚拟按钮，无需先手动绑定（P1）
- SyncVkButtonBindings 不破坏右键菜单建立的绑定（P1）
- 右击按钮时只显示按钮菜单，不弹出空白区域菜单（P3）

**Non-Goals:**
- 不修改现有键盘快捷键（RegisterHotKey）触发路径
- 不支持为单个虚拟按钮指定不同目标（始终全局目标）
- 不修改 MacroSequence 的 TargetAppPath 字段用途
- P1 不改变 SyncVkButtonBindings 的整体结构（仅改全量覆盖逻辑）
- P3 不引入新的 UI 组件，仅修复现有菜单冒泡问题

## Decisions

### 决策 1：目标捕获方式——隐藏→延时→捕获前台

**做法**：
1. 用户点击右键菜单"捕获目标窗口"
2. VK 窗口隐藏自身（`Visible = false`）
3. 启动 3 秒倒计时 + 半透明提示窗口（显示"请在 3 秒内切换到目标窗口..."）
4. 倒计时结束 → `GetForegroundWindow()` 获取前台窗口句柄
5. `GetWindowThreadProcessId()` → 获取进程 ID → `Process.ProcessName` 获取进程名
6. 同时获取窗口标题 (`GetWindowText`)
7. 恢复 VK 窗口显示
8. 将进程名 + 标题持久化到 LayoutData

**备选方案对比**：
| 方案 | 优点 | 缺点 |
|------|------|------|
| 隐藏+3秒捕获（选定） | 用户主动控制，命中率 100% | 需要倒计时，体验略绕 |
| 常驻监听（VK 激活时记录上一个前台） | 无额外操作 | 用户可能先激活 VK 再切走，捕获到的是无关窗口 |
| 手动输入进程名 | 简单快速 | 用户要记住/查找进程名，不直观 |

### 决策 2：键盘消息采用 PostMessage 而非 SendMessage

`SendMessage` 同步等待目标窗口处理消息，若目标窗口主线程阻塞会导致调用方卡死。
`PostMessage` 异步投递到消息队列后立即返回，更安全。

修饰键序列构造规则：
```
Ctrl+S（VK_CONTROL=0x11, VK_S=0x53）
  → PostMessage(hWnd, 0x0100, 0x11, MakeLParam(0x1D, false, false))  // WM_KEYDOWN Ctrl
  → PostMessage(hWnd, 0x0100, 0x53, MakeLParam(0x1F, false, false))  // WM_KEYDOWN S
  → PostMessage(hWnd, 0x0101, 0x53, MakeLParam(0x1F, false, true ))  // WM_KEYUP   S
  → PostMessage(hWnd, 0x0101, 0x11, MakeLParam(0x1D, false, true ))  // WM_KEYUP   Ctrl
```

文本输入通过 `WM_CHAR`：
```
PostMessage(hWnd, 0x0102, 'h', 0)  // WM_CHAR
PostMessage(hWnd, 0x0102, 'e', 0)
...
```

`lParam` 构造：
```
bit 0-15:  重复计数（通常 1）
bit 16-23: ScanCode（MapVirtualKey 获取）
bit 24:    扩展键标志（箭头、Ins/Del 等设为 1）
bit 29-30: 上下文码（KeyDown 为 0, KeyUp 为 1）
bit 31:    过渡状态（KeyUp 为 1）
```

### 决策 3：方案 A 失败时自动降级到方案 B

播放流程：

```
OnButtonClicked
  ↓
有目标窗口？
  ├── No  → player.Play(seq)          // 维持现有行为
  └── Yes →
       PostMessage 方案尝试
       ↓
       判断目标是否为当前前台？
         ├── 是前台 → player.Play(seq)    // 本身就是目标，无需 PostMessage
         └── 非前台 → player.PlayToWindow(seq, hWnd)
                      ↓ 播放完成后
                      GetForegroundWindow() 是否为目标？
                        ├── 是 → 方案 A 成功 ✅
                        └── 否 → 方案 A 失败，记录失败标记
                                下次自动用方案 B
```

方案 A 失败一次后，同一会话中自动切换到方案 B。

### 决策 4：目标窗口匹配策略

按以下优先级匹配：
1. 窗口句柄缓存（当前进程内直接可用）
2. 按进程名查找第一个匹配窗口
3. 若有窗口标题，在进程内按标题精确匹配

句柄有效性通过 `IsWindow()` 检查。

### 决策 5（P1）：VkPickMode 改为双向填充

当前 `ReceiveVkHotkey` 只填充 `_txtHotkey`，改为 `ReceiveVkPick(buttonName, hotkey?)` 同时填充 `_txtVkBind` 和 `_txtHotkey`。

进入 VkPickMode 的前置条件从 `HasBoundButtons()`（需有按钮已绑定）简化为 VK 窗口存在于前台即可。

数据类型流向：
```
VK 窗口点击按钮
  ↓
OnButtonClicked 检测 IsVkPickMode
  ↓
获取按钮名 → vbtn.Name
获取热键  → ResolveBinding → seq?.TriggerHotkey
  ↓
ReceiveVkPick(buttonName, hotkey)
  ↓
SequenceEditor:
  _txtVkBind.Text = buttonName    ← 新增
  _txtHotkey.Text = hotkey ?? ""  ← 原行为扩展
  IsVkPickMode = false
```

### 决策 6（P1）：SyncVkButtonBindings 改为不破坏已有绑定

当前逻辑全量覆盖：
```csharp
vbtn.BindActionId = seq?.Id; // 不匹配时设为 null，破坏右键绑定
```

改为仅更新匹配上的，不匹配的不动：
```csharp
if (seq != null) vbtn.BindActionId = seq.Id;
// 不留 null —— 不匹配时保持 vbtn.BindActionId 原值
```

### 决策 7（P3）：VirtualButtonWidget 设空 ContextMenuStrip 阻断冒泡

根因：
1. VirtualButtonWidget.OnMouseClick 检测到右键 → 触发 ContextMenuRequested → OnWidgetContextMenu 显示按钮菜单
2. WinForms 同时处理 WM_CONTEXTMENU，发现 widget.ContextMenuStrip == null → 向上遍历到父 FlowLayoutPanel → 显示 blankMenu
3. 两个菜单同时出现

修复：在 VirtualButtonWidget 构造函数中设置空的 ContextMenuStrip：
```csharp
ContextMenuStrip = new ContextMenuStrip();
```
- WinForms 现在发现 widget 有自己的 ContextMenuStrip（空），停止冒泡
- 空的菜单条不会渲染任何可见内容
- 自定义 OnWidgetContextMenu 不受影响（通过 menu.Show() 直接显示，不依赖 WinForms 冒泡机制）

## Risks / Trade-offs

| 风险 | 程度 | 缓解措施 |
|------|------|----------|
| **Spine（Java AWT）不响应 PostMessage** | ⚠️ 中高 | 自动降级到方案 B（激活窗口+SendKeys），添加日志便于诊断 |
| **PostMessage 修饰键在 Java 中失效** | ⚠️ 中 | Java 的 FocusManager 可能检查前台状态。WM_KEYDOWN 带正确 lParam 可降低此风险 |
| **玩家点了按钮但忘记了上次的目标** | ✅ 低 | 状态栏显示当前目标名，右键菜单标明当前目标 |
| **目标进程关闭后重开句柄失效** | ✅ 低 | 每次点击重新按进程名查找，不依赖缓存的句柄 |
| **方案 B 窗口闪烁影响操作流** | ⚠️ 中 | 在 VK 窗口状态栏显示当前模式，让用户预期行为 |
