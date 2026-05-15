## Why

虚拟按键窗口经历了完全重写后，存在 10 个遗留问题，涵盖渲染缺陷、布局异常、交互缺失和持久化不足。本次变更一次性修复全部问题，并补全缺失的导入导出功能。

## What Changes

- **P1**: 修复按钮 PNG 贴图圆角外透明区域显示黑色的问题，消除窗口背景 9-slice 贴图的粉色边缘线
- **P2**: 循环按钮的计数器输入框背景色从硬编码黑色改为匹配皮肤颜色
- **P3**: 主窗口 DataGridView 最右列不再留空，自动填充剩余宽度
- **P4**: 修复 VK 窗口首次打开时边缘轮廓异常（增删按钮后自动恢复），添加 Shown 事件触发重算
- **P5**: 按钮 UI 重绘——移除大号首字图标，名称字号加大，颜色改为白色
- **P6**: 增删按钮时强制刷新 panel 消除贴图残留
- **P7**: 将无用的"位置锁定"改为真正的拖拽排序功能
- **P8**: 移除右键菜单中无意义的"保存布局"和"重置布局"
- **P9**: 按钮右键菜单新增"增加间隔"，插入固定像素空白分隔条（随缩放等比变化）
- **P10**: 主界面增加导入/导出按钮，将 Spine 热键编辑、序列设置、VK 布局、VK 设置统一到单一 JSON 文件，导入时分项确认

## Capabilities

### New Capabilities
- `data-import-export`: 统一导入导出机制，支持 Spine 热键、序列、VK 布局和 VK 设置的整体打包导出与分项确认导入

### Modified Capabilities
- `widget-context-menu`: 右键菜单新增"增加间隔"选项，移除"保存布局"和"重置布局"
- `vk-pick-mode-binding`: 按钮位置锁定功能改为拖拽排序

## Impact

- [VirtualButton.cs](KeyMacro/Models/VirtualButton.cs): 新增 `IsSpacer` 和排序相关字段
- [VirtualButtonManager.cs](KeyMacro/Services/VirtualButtonManager.cs): 新增 `MoveButton` 和 `AddSpacerAfter` 方法
- [VirtualKeyWindow.cs](KeyMacro/Forms/VirtualKeyWindow.cs): 渲染、布局、菜单、拖拽、Shown 事件多处修改
- [VirtualButtonWidget.cs](KeyMacro/Forms/VirtualButtonWidget.cs): 绘制逻辑重做、TextBox 颜色、spacer 支持
- [MainForm.cs](KeyMacro/Forms/MainForm.cs): 列宽修复、新增导入导出按钮
- [SpineHotkeyEditor.cs](KeyMacro/Forms/SpineHotkeyEditor.cs): 支持从数据结构（而非仅文件）加载
- [DataBundle.cs](KeyMacro/Models/DataBundle.cs): 新增导入导出数据模型
- [DataBundleService.cs](KeyMacro/Services/DataBundleService.cs): 新增导入导出服务
