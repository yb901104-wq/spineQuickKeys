## Why

虚拟按钮改名后已绑定的序列不同步（`TriggerVkButtonName` 仍为旧名），且从窗口管理器点"显示"打开窗口时传入残缺数据（无 Buttons/SkinPath），导致窗口内容丢失。

## What Changes

- 修改按钮改名流程，同步更新匹配序列的 `TriggerVkButtonName` 并触发 `SyncVkButtonBindings`
- 修改窗口管理器"显示"按钮逻辑，从 JSON 加载完整 `WindowLayoutData` 再传递

## Capabilities

### New Capabilities
<!-- 无新能力引入，仅修复现有行为 -->

### Modified Capabilities
<!-- 无 spec 级行为变更 -->

## Impact

- `VirtualKeyWindow.cs` — 改名处理增加序列同步
- `VkWindowManager.cs` — "显示"按钮传递完整数据而非残缺对象