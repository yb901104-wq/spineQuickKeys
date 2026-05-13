## 1. 右键菜单分离（P3）

- [x] 1.1 VirtualButtonWidget 构造函数设置空 ContextMenuStrip 阻断冒泡

## 2. VkPickMode 绑定优化（P1）

- [x] 2.1 SequenceEditor 移除 VkPickMode 的 HasBoundButtons 前置条件
- [x] 2.2 将 ReceiveVkHotkey 改为 ReceiveVkPick，接收按钮名和热键，同时填充 `_txtVkBind` 和 `_txtHotkey`
- [x] 2.3 VirtualKeyWindow.OnButtonClicked 的 VkPickMode 分支改为始终获取按钮名，有绑定则同时取热键
- [x] 2.4 MainForm.SyncVkButtonBindings 改为不匹配时保留原有 BindActionId（不设 null）

## 3. Layout 数据模型扩展（P2）

- [x] 3.1 VirtualLayoutSerializer.LayoutData 增加 TargetProcessName 和 TargetWindowTitle 属性

## 4. 目标窗口捕获（P2）

- [x] 4.1 VirtualKeyWindow 空白右键菜单增加"捕获目标窗口"菜单项
- [x] 4.2 实现捕获流程：VK 窗口隐藏 → 3 秒倒计时 → GetForegroundWindow 捕获前台
- [x] 4.3 捕获后获取进程名和窗口标题，保存到 LayoutData
- [x] 4.4 右键菜单增加"清除目标窗口"（仅在已设定目标时显示）
- [x] 4.5 目标窗口解析工具方法：按进程名/标题查找窗口句柄

## 5. 方案 A：PlayToWindow PostMessage 实现（P2）

- [x] 5.1 MacroPlayer 新增 PlayToWindow 方法（不含 500ms 初始延迟）
- [x] 5.2 实现 PostKey：向目标窗口发送 WM_KEYDOWN + WM_KEYUP（含正确 lParam 构造）
- [x] 5.3 实现 PostCombo：向目标窗口发送修饰键 + 按键序列
- [x] 5.4 实现 PostText：向目标窗口发送 WM_CHAR 消息序列
- [x] 5.5 支持 PressMode.Hold 的 PostMessage 版本

## 6. 方案 B：自动激活目标窗口（P2）

- [x] 6.1 VirtualKeyWindow.OnButtonClicked 增加目标窗口判断逻辑
- [x] 6.2 实现激活流程：SetForegroundWindow → 200ms 延时 → Play()
- [x] 6.3 处理目标进程不存在时的静默降级

## 7. 方案优先级与自动降级（P2）

- [x] 7.1 实现方案发现逻辑：优先级标记管理（每个会话一次）
- [x] 7.2 OnButtonClicked 整合完整流程：VkPickMode → Loop → 方案 A → 方案 B → 回退
- [x] 7.3 PlayToWindow 执行后检测目标是否为前台，若非前台则标记方案 A 失败
