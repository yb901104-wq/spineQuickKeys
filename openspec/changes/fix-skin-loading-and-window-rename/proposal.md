## Why

虚拟按键窗口的皮肤 PNG 图片完全未加载，按钮全用 GDI+ 绘制，导致视觉不一致且无法使用自定义皮肤。同时 VkWindowManager 不支持修改窗口名，无法区分多窗口场景。

## What Changes

- 修复 `VkSkinLoader` 磁盘路径：缺少 `KeyMacro/skins/` 前辍导致开发模式下永远找不到皮肤目录
- 修复 `VkSkinLoader` 嵌入式资源加载：确认 `.csproj` 编译配置确保 PNG 被打入程序集
- 补齐 `SpineSkin` 缺少的 pressed/active 状态 PNG
- VkWindowManager 的"窗口名称"列改为可编辑，修改后自动持久化

## Capabilities

### New Capabilities
- `skin-loading`: 皮肤资源加载路径修复，确保开发模式和发布模式均能正确加载 PNG
- `window-rename`: VkWindowManager 中支持在线修改窗口名称

### Modified Capabilities

<!-- 无现有 spec 变更 -->

## Impact

- `VkSkinLoader.cs`：磁盘路径解析逻辑
- `VkWindowManager.cs`：列 ReadOnly 改为 false，CellValueChanged 处理命名
- `KeyMacro.csproj`：可能需调整 EmbeddedResource 配置
- `skins/SpineSkin/`：新增 pressed/active PNG 文件
