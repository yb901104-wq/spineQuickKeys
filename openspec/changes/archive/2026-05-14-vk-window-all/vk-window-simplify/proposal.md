## Why

虚拟按键窗口的缩放和工具栏交互问题反复修复未果。缩放手柄与 ScaleFactor 互相冲突导致窗口跳动；工具栏浮动覆盖影响按钮位置。简化方案：砍掉缩放手柄，工具栏改标题栏，纯菜单缩放。

## What Changes

- 删除缩放手柄及所有相关代码
- 工具栏改为 Dock=Top 标题栏，锁定隐藏，窗口自动缩短
- 删除所有窗口最小/最大尺寸限制
- 右键缩放菜单增加自定义百分比输入（≥10%）
- RecalculateSize 简化：标题栏高 + padding + 按钮内容
- 保存 ScaleFactor 的 layout 迁移代码

## Capabilities

### New Capabilities
- `window-simplify`: 虚拟按键窗口简化——去除缩放手柄、标题栏 Dock=Top、无尺寸限制、菜单缩放

## Impact

| 文件 | 改动 |
|------|------|
| Forms/VirtualKeyWindow.cs | 删除 resizeGrip、恢复 toolbar Dock=Top、删除最小尺寸、增加自定义缩放、简化 RecalculateSize |
