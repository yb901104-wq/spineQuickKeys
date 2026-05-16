## Why

第一个窗口皮肤加载不上，按钮识别应为 "窗口名+按钮名" 复合键而非全窗口唯一。

## What Changes

- ToggleWindowVisibility 显示窗口前从 serializer 刷新 _data 再 ReloadSkin
- VkPickMode 写入 "窗口名/按钮名" 复合键
- SyncVkButtonBindings 优先复合匹配，再回退单名匹配
- 移除 CheckButtonNameExists 全局唯一校验

## Impact

- MainForm.cs, VirtualKeyWindow.cs, SequenceEditor.cs
