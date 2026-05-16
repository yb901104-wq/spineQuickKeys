## Context

当前 `VkSkinLoader` 在构造函数中尝试解析皮肤磁盘路径：

```
CWD/skins/{name}          → 开发时 CWD 为解决方案根，缺少 KeyMacro/ 前缀
AppContext.BaseDir/skins/{name}  → bin 目录下无 skins/
```

两条路径都找不到磁盘文件，完全依赖嵌入式资源。但嵌入式资源也可能有编译或命名问题，导致空加载。

同时 `VkWindowManager` 中"窗口名称"列是 `ReadOnly = true`，用户无法直接编辑。

## Goals / Non-Goals

**Goals:**
- 开发模式（`dotnet run --project KeyMacro`）下能从磁盘加载皮肤
- 嵌入式资源路径验证并修复
- VkWindowManager 支持直接编辑窗口名称并持久化

**Non-Goals:**
- 不涉及皮肤热加载或运行时切换皮肤
- 不涉及 VkWindowManager 的其他列编辑（目标、按钮数等）
- 重命名不涉及跨窗口序列绑定同步（只改显示名）

## Decisions

### 1. 磁盘路径增加 KeyMacro/ 前缀
`VkSkinLoader` 构造函数中，在现有两条路径之前增加：
```
{CWD}/KeyMacro/skins/{name}
```
`dotnet run --project KeyMacro` 时 CWD 为解决方案根，拼接 `KeyMacro/skins/SpineSkin` 即为正确路径。

同时保留原有两条路径作为 fallback，覆盖其他启动场景。

### 2. 嵌入式资源命名不变
当前 `.csproj` 中 `<EmbeddedResource Include="skins\**" />` 将 `KeyMacro/skins/SpineSkin/btn_small_normal.png` 编译为 `KeyMacro.skins.SpineSkin.btn_small_normal.png`。`OpenEmbedded` 中拼接 `KeyMacro.skins.{_skinName}.{fileName}`，命名匹配。
但需在开发环境实测验证，确认无错。

### 3. 窗口重命名直接编辑列
不要弹出对话框，直接让列可编辑即可：
- DataGridView 列 0 `ReadOnly = false`
- `CellValueChanged` 中检测 column index 0
- 更新 `_serializer` 中的对应窗口数据
- 同步更新 `VirtualKeyWindow._data.Name`
- 因为窗口名称是关联 key（`Tag` 绑定），需同时更新行 `Tag`

### 4. 行 Tag 同步策略
改名时需要同时更新 `DataGridViewRow.Tag` 和全局数据中的 `Name`，否则后续显示/删除操作会找不到窗口。采用先改数据再刷新列表的策略。

## Risks / Trade-offs

- [窗口名作为关联 key] 当前代码以 `Name` 作为窗口唯一标识（`_dgv.Rows[row].Tag = w.Name`），改名后用同一标识查找可能失灵。需改为 immutalbe ID 或改名时同步所有关联。
  → 处理方案：改名后立即刷新全局的 name 索引，调用 `RefreshList()` 重建列表。
