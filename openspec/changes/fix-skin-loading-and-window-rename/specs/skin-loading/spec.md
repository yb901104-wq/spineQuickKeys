## ADDED Requirements

### Requirement: 皮肤磁盘路径支持 KeyMacro/ 子目录
系统 SHALL 在 `VkSkinLoader` 的磁盘路径搜索中加入 `{CWD}/KeyMacro/skins/{name}`。
系统 MUST 保留原有 `{CWD}/skins/{name}` 和 `{AppContext.BaseDirectory}/skins/{name}` 作为 fallback。

#### Scenario: 开发模式从解决方案根启动
- **WHEN** 用户从 `d:\AIAGENT\spineQuickKeys`（解决方案根）执行 `dotnet run --project KeyMacro`
- **THEN** `VkSkinLoader` 找到 `KeyMacro/skins/SpineSkin/` 下的 PNG 文件
- **THEN** 按钮使用 PNG 正常状态图片绘制，非 GDI+ 回退

#### Scenario: 皮肤目录不存在时回退 GDI+
- **WHEN** `skinPath` 对应的目录在三條路徑下都不存在
- **THEN** 系统正常回退到 GDI+ 硬编码绘制，不抛异常

### Requirement: 补齐 SpineSkin pressed/active 状态图片
SpineSkin 目录 SHALL 包含以下图片文件：
- `btn_small_pressed.png`
- `btn_small_active.png`
- `btn_large_pressed.png`
- `btn_large_active.png`
- `btn_loop_pressed.png`
- `btn_loop_active.png`

#### Scenario: 按钮按下时显示 pressed 图片
- **WHEN** 用户左键按下皮肤按钮
- **WHEN** `btn_{style}_pressed.png` 存在
- **THEN** 按钮绘制 pressed 状态图片
