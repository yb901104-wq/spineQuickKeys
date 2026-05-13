## Why

虚拟按键窗口（VirtualKeyWindow）目前存在三个未解决问题：

1. **P1：VkPickMode 绑定断裂**——SequenceEditor 的"录制触发快捷键"入口的 VkPickMode 存在循环依赖（需要按钮已绑定）、不填充关联虚拟按键字段（用户要手动输入按钮名），且 SyncVkButtonBindings 全量覆盖 BindActionId 破坏右键菜单建立的绑定
2. **P2：键盘模拟发错窗口**——点击虚拟按钮时 SendKeys/keybd_event 发到 VK 窗口自身而非目标软件（Spine），因 Windows 在按钮点击前已切换焦点
3. **P3：右键菜单冲突**——右击按钮时按钮菜单和空白区域菜单同时弹出，因 VirtualButtonWidget 未设 ContextMenuStrip，WinForms 自动冒泡到父面板

## What Changes

**P1：VkPickMode 修复**
- 移除 HasBoundButtons 前置条件，VK 窗口存在即可进入 VkPickMode
- 点击虚拟按钮后同时填充触发快捷键和关联虚拟按键字段
- SyncVkButtonBindings 改为不破坏已有绑定（不匹配时保持 BindActionId 不变）
- SequenceEditor 新增"选择虚拟按键"按钮，一步进入选键模式

**P2：目标窗口绑定**
- 为 VK 窗口增加"目标窗口捕获"能力——用户设定一个目标软件窗口，所有虚拟按钮自动定向
- **方案 A（实验性）**：MacroPlayer 新增 PostMessage 路径，直接向目标窗口注入键盘消息，完全绕过焦点切换
- **方案 B（稳定）**：VK 窗口在播放序列前自动激活目标窗口，复用现有 SendKeys
- 两方案共存并自动降级：优先方案 A，若不响应则回退到方案 B

**P3：右键菜单分离**
- VirtualButtonWidget 构造函数设置空的 ContextMenuStrip，阻止 WinForms 冒泡到父面板
- 按钮右键菜单和空白区域右键菜单互不干扰

## Capabilities

### New Capabilities
- `target-window-capture`: 目标窗口捕获与持久化——用户通过右键菜单启动捕获流程，将指定窗口设为虚拟按键的目标
- `background-key-injection`: 后台键盘消息注入——用 PostMessage 直接向目标窗口发键盘消息，不需要窗口在前台
- `auto-activate-target`: 自动激活目标窗口——播放序列前自动激活目标窗口，确保 SendKeys 发到正确窗口
- `vk-pick-mode-binding`: VkPickMode 绑定优化——移除前置依赖，点击虚拟按钮后同步填充触发快捷键和关联虚拟按键字段
- `widget-context-menu`: 虚拟按钮右键菜单修正——阻断右键菜单向父面板冒泡

### Modified Capabilities
<!-- No existing specs are modified -->

## Impact

| 文件 | 改动 | 关联 |
|------|------|------|
| Services/VirtualLayoutSerializer.cs | LayoutData 增加 TargetProcessName、TargetWindowTitle | P2 |
| Services/MacroPlayer.cs | 新增 PlayToWindow(sequence, hWnd) PostMessage 路径 | P2 |
| Forms/VirtualKeyWindow.cs | VkPickMode 改为填充 buttonName；捕获目标窗口菜单；OnButtonClicked 目标窗口逻辑 | P1, P2 |
| Forms/MainForm.cs | SyncVkButtonBindings 改为不破坏已有绑定 | P1 |
| Forms/SequenceEditor.cs | 移除 HasBoundButtons 前置条件；新增 ReceiveVkPick；添加"选择虚拟按键"按钮 | P1 |
| Forms/VirtualButtonWidget.cs | 构造函数加 ContextMenuStrip = new ContextMenuStrip() | P3 |
