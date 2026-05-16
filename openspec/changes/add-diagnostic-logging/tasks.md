## 1. VirtualKeyWindow 日志

- [x] 1.1 OnButtonClicked 入口日志：按钮名、BindActionId、IsPlaying、VkPickMode
- [x] 1.2 目标窗口解析日志后立即记录方案选择（ DirectPlay / PostMessage / ActivateWindow ）
- [x] 1.3 ResolveTargetWindow 中记录进程查找结果

## 2. VirtualKeyBindingManager 日志

- [x] 2.1 ResolveBinding 记录 BindActionId 和查找结果（序列名/未找到）

## 3. MainForm 日志

- [x] 3.1 SyncVkButtonBindings 记录匹配统计

## 4. 验证

- [x] 4.1 编译通过，0 错误
- [ ] 4.2 确认日志中出现 `[DIAG]` 标记（需手动点击 VK 按钮触发）
