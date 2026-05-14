## Why

虚拟按键窗口目前使用标准 Windows 窗口样式（Sizable 边框+标题栏），与输入法风格的浮动窗口期望不符。用户希望窗口更轻量、可定制外观、布局灵活，并支持自定义美术资源。

## What Changes

- **无标题栏窗口**：`FormBorderStyle = None`，移除标准边框和标题栏，改为自定义绘制
- **工具栏**：解锁时显示工具栏（目标窗口名+按钮数+关闭按钮），锁定后隐藏，可拖拽移动窗口
- **右下角缩放手柄**：拖拽时同步缩放按钮（ScaleFactor），与右键菜单缩放联动
- **布局模式切换**：右键菜单"单排/多排"切换，单排 AutoSize 水平撑开，多排固定宽度换行
- **缩放菜单**：右键菜单增加预设比例（50%/75%/100%/150%/200%）
- **自定义皮肤系统**：skins/\<name\>/ 目录 + skin.json 颜色配置 + PNG 图标 + 9-slice 贴图，无图回退 GDI+
- **关闭窗口**：右键菜单"关闭窗口"选项
- **窗口锁定**：锁定后隐藏工具栏，禁止拖拽

## Capabilities

### New Capabilities
- `borderless-window`: 无标题栏窗口模式——FormBorderStyle=None + 工具栏 + 右下角缩放手柄 + 窗口锁定/关闭
- `layout-mode-switch`: 布局模式切换——单排（WrapContents=false + AutoSize）与多排（固定宽度 + 换行）可切换
- `custom-skin-system`: 自定义皮肤系统——skin.json 配色 + PNG 图标 + 9-slice 贴图，无资源时回退 GDI+

### Modified Capabilities
- `widget-context-menu`: 右键菜单新增缩放预设值、单排/多排切换、关闭窗口

## Impact

| 文件 | 改动 |
|------|------|
| Forms/VirtualKeyWindow.cs | FormBorderStyle=None、工具栏、缩放手柄、布局模式切换、新增右键菜单项 |
| Forms/VirtualButtonWidget.cs | 新增皮肤渲染路径（从 VkSkinLoader 获取图片代替 GDI+ 绘制） |
| Services/VkSkinLoader.cs | **新增** 皮肤加载器：从目录加载 skin.json + PNG，提供默认回退 |
| Services/VirtualLayoutSerializer.cs | LayoutData 增加 SkinPath、ScaleFactor 持久化 |
| Models/VirtualButton.cs | 可能增加 IconCustomPath 字段 |
| (新文件) | 皮肤资源规范文档 (SKIN_GUIDE.md) |
