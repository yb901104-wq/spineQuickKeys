## Why

当前虚拟按键窗口只支持单个实例，用户需要多个独立 VK 窗口分别绑定不同目标程序、不同按钮布局。需引入多窗口管理机制。

## What Changes

- **VirtualLayoutSerializer 重写**：`LayoutData` 改为 `WindowLayoutData`（单窗口数据），`GlobalLayoutData` 包裹 `List<WindowLayoutData>`；旧格式自动迁移
- **VirtualKeyWindow 解耦**：不再接收外部的 `VirtualButtonManager`/`VirtualLayoutSerializer`，而是接收自己的 `WindowLayoutData`，内部自管按钮和序列化
- **MainForm 改造**：`_vkBtnManager`/`_vkWindow` 改为 `List<VirtualKeyWindow>`；"开启虚拟按键"遍历 enabled 窗口统一显示；"关闭虚拟按键"隐藏所有
- **VkWindowManager 新增**：管理窗口 Form，列出所有窗口（名称/目标/按钮数/允许显示 checkbox/显示隐藏按钮/删除按钮）；支持添加新窗口和删除窗口
- **VK 窗口右键菜单**：新增「删除当前窗口」项（彻底删除+关闭）；原有「关闭窗口」保持隐藏语义不变
- **布局持久化**：`enabled` 字段记录窗口是否允许显示

## Capabilities

### New Capabilities
- `multi-vk-window`: 多虚拟按键窗口支持，包含窗口管理器和独立窗口生命周期

### Modified Capabilities
- (无现有 spec 需要修改)

## Impact

- [VirtualLayoutSerializer.cs](KeyMacro/Services/VirtualLayoutSerializer.cs): 重构数据模型，支持多窗口列表存储和旧格式自动迁移
- [VirtualKeyWindow.cs](KeyMacro/Forms/VirtualKeyWindow.cs): 构造函数改为接收 `WindowLayoutData`；新增 `WindowData` 属性和 `SaveSelf()` 方法；右键菜单新增「删除当前窗口」
- [MainForm.cs](KeyMacro/Forms/MainForm.cs): `_vkBtnManager`/`_vkSerializer`/`_vkWindow` 改为 `List<VirtualKeyWindow>`；"开启/关闭"逻辑重写；`SyncVkButtonBindings` 遍历所有窗口
- [VkWindowManager.cs](KeyMacro/Forms/VkWindowManager.cs): 新增管理窗口 Form
- [VirtualKeyBindingManager.cs](KeyMacro/Services/VirtualKeyBindingManager.cs): 可能需小调整适配多窗口
