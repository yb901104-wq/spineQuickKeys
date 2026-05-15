## Why

上一个变更（V2.01 DPI 修复）只覆盖了主窗口和 VK 窗口的缩放，SequenceEditor 仍有大量硬编码尺寸未适配 DPI。同时 Spine 热键路径每次需要重新选择、虚拟按钮文字比例偏大、快捷键和虚拟按键绑定存在冗余机制、主窗口表格列宽不支持缩放。5 个问题一次性修复。

## What Changes

- **问题 1 - SequenceEditor DPI**: 扩展 `ApplyDpiScale()` 覆盖步骤表格列宽、工具栏高度、底部面板、自动补全下拉框、字号等所有硬编码尺寸
- **问题 2 - Spine 热键路径持久化**: ConfigService 新增 `AppConfig` 包裹类型，存储 `LastSpineFilePath`。打开 Spine 编辑器时若路径有效则跳过文件选择对话框
- **问题 3 - 虚拟按钮文字比例**: 字号从固定 `Scaled(n)` 改为基于按钮高度的比例计算（如按钮高度 × 0.25），保持视觉比例恒定
- **问题 4 - 双向绑定 + 移除冗余**: 序列可同时绑定快捷键和虚拟按键（已支持）。移除 VK 按钮右键菜单的「绑定快捷键」及其子菜单。`SyncVkButtonBindings` 作为唯一绑定机制
- **问题 5 - 主窗口列宽自适应**: DataGridView 的 `AutoSizeColumnsMode` 从 `None` 改为 `Fill`，或按比例分配宽度响应窗口缩放

## Capabilities

### New Capabilities
- `app-config`: 应用级配置（非序列数据），如 Spine 最近文件路径
- `main-grid-auto-resize`: 主窗口序列列表列宽随窗口缩放自适应

### Modified Capabilities
- `widget-context-menu`: 移除「绑定快捷键」菜单项及子菜单
- `dpi-aware-layout`: 将 DPI 感知覆盖到 SequenceEditor 全部控件

## Impact

- [ConfigService.cs](KeyMacro/Services/ConfigService.cs): 新增 `AppConfig` 配置模型（含 `LastSpineFilePath`、`Sequences`），Load/Save 改读写包裹对象
- [MainForm.cs](KeyMacro/Forms/MainForm.cs): 列宽改为 `Fill` 模式或按比例分配；`OpenSpineEditor()` 路径记忆；移除 `SyncVkButtonBindings` 中对 VK 按钮绑定覆盖的副作用（见问题 4）
- [VirtualKeyWindow.cs](KeyMacro/Forms/VirtualKeyWindow.cs): 移除右键菜单「绑定快捷键」项及其子菜单项。注意：保留 `ShowBindingDialog` 和 `VirtualKeyBindingManager`（可能用于其他用途），但菜单入口移除
- [VirtualButtonWidget.cs](KeyMacro/Forms/VirtualButtonWidget.cs): 字号 `Scaled(x)` 改为按按钮高度比例计算
- [SequenceEditor.cs](KeyMacro/Forms/SequenceEditor.cs): `ApplyDpiScale()` 扩展覆盖步骤表格、工具栏、底部面板、下拉框
