## Why

虚拟按键窗口的尺寸逻辑存在循环依赖（ScaleFactor ↔ 窗口尺寸互相决定），导致窗口不随按钮增删自适应、缩放跳跃、工具栏影响按钮位置、缩放手柄被遮挡不可用。

## What Changes

- **A: 尺寸逻辑重写** — ScaleFactor 作为唯一数据源，窗口尺寸始终由内容计算。Resize 事件不再调 UpdateScale，切断循环。拖拽时实时缩放预览，松手吸附。单排不恢复旧尺寸，只恢复 ScaleFactor。LayoutData 新增 ScaleFactor 持久化。
- **B: 工具栏隐藏不占空间** — 不切换 Visible，改为 Height 0↔28，面板布局不动。
- **C: 工具栏拖拽 + 缩放手柄 Z 序** — Label 挂载拖拽事件；Controls 顺序调整，resizeGrip 放最上层。

## Capabilities

### New Capabilities
- `window-auto-size`: 窗口自动尺寸——窗口始终由按钮内容 + ScaleFactor 决定，按钮增减时自适应

### Modified Capabilities
<!-- No existing specs modified -->

## Impact

| 文件 | 改动 |
|------|------|
| Forms/VirtualKeyWindow.cs | Resize 事件、RecalculateSize、工具栏拖拽/隐藏、缩放手柄 Z 序、拖拽逻辑 |
| Services/VirtualLayoutSerializer.cs | LayoutData 增加 ScaleFactor 字段 |
