## Why

应用在 3200×2000 / 200% 缩放的 Win11 笔记本上所有界面都挤在一起、控件互相侵占空间。原因是所有布局都使用硬编码像素值（48px 按钮高度、4px 间距、10pt 字体等），绕过了 WinForms 的 DPI 自动缩放机制，导致 200% 缩放下控件大小只有应有的一半，文字却以 2x 渲染。

## What Changes

- **VirtualKeyWindow**: 布局常量（BASE_BTN_H, BASE_GAP, BASE_MARGIN, BaseBtnWidth）乘以系统 DPI 系数；系统 DPI 与用户 `_scaleFactor` 合并为单一缩放因子；`DpiChanged` 事件重算布局
- **VirtualButtonWidget**: `UpdateSize()` 和字号改用实际缩放因子（DPI × _scaleFactor），而非硬编码像素
- **MainForm**: 工具栏 DataGridView 的 `Padding(0,48,0,0)` 改为动态计算；添加 `DpiChanged` 事件刷新
- **SequenceEditor**: TableLayoutPanel 固定行高（28, 42, 32）和固定列宽（130）改为 DPI 感知
- 移除或合并两套平行缩放机制（系统 DPI 缩放 + 用户缩放 _scaleFactor），改为单一缩放因子

## Capabilities

### New Capabilities
- `dpi-aware-layout`: DPI 感知布局能力，覆盖所有窗口和控件的尺寸/间距/字体计算

### Modified Capabilities
- 无（当前无现有 spec）

## Impact

- 修改文件：VirtualKeyWindow.cs, VirtualButtonWidget.cs, MainForm.cs, SequenceEditor.cs
- 无 API 变更，无依赖变更
- 布局文件 `virtual_layout.json` 存储的 `ScaleFactor` 含义不变（用户缩放），系统 DPI 不持久化、运行时动态获取
