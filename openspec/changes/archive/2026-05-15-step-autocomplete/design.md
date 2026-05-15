## Context

SequenceEditor 的步骤表格中，用户编辑"按键/文本"列时目前只能手动输入或通过"录制按键"按钮。Spine 热键文档已包含大量常用快捷键，但无法在编辑时直接检索引用。本功能集成 Spine 热键数据到步骤编辑流程中。

## Goals / Non-Goals

**Goals:**
- 在步骤表格编辑"按键/文本"单元格时，弹出搜索下拉菜单
- 搜索自动匹配英文名、中文翻译或按键值
- 选中后填入快捷键组合（Keys 字段）
- 仅在有已载入的 Spine 热键文档时启用

**Non-Goals:**
- 不修改 Spine 热键文档的加载/编辑逻辑
- 不做全文索引或模糊匹配（仅包含匹配）
- 不修改非步骤表格的其他输入框

## Decisions

### 数据共享：SpineHotkeyEditor 暴露静态属性

SequenceEditor 需要访问 Spine 热键条目，但两者无直接引用关系。通过静态属性传递：

```csharp
// SpineHotkeyEditor 新增
public static List<SpineHotkeyEntry>? LastLoadedEntries { get; private set; }
// 在 LoadEntries 和构造器数据加载时设置
```

SequenceEditor 通过 `SpineHotkeyEditor.LastLoadedEntries` 读取。

### 搜索分流逻辑

在 TextChanged 事件中判断输入内容：
- 含中文（Regex.IsMatch(input, @"\p{IsCJKUnifiedIdeographs}")）→ 在 ChineseNote 中搜索
- 含 `+` → 在 Keys 中搜索
- 其他 → 在 Name 中搜索

### 弹出下拉控件

使用一个 `ListBox` 控件，在单元格编辑时显示在 DataGridView 下方：
- 由自定义 `EditingControl` 的 TextChanged 触发
- ListBox 定位在单元格下方（屏幕坐标）
- 鼠标点击或 Enter 键选择
- Esc 键关闭下拉不填充

### 选中结果的处理

选中条目后：
1. 取该条目的 `Keys` 字段值
2. 填入当前编辑的单元格
3. 检测 `Keys` 值是否包含 `+`：
   - 包含 → 同步将步骤类型列设为"组合键"
   - 不包含 → 如果当前类型是"文本"则设为"单键"
4. 关闭下拉

## Risks / Trade-offs

- [ListBox 与 DataGridView 坐标] → 需计算单元格屏幕位置，滚动时需隐藏下拉。使用 CellEndEdit 事件自动关闭。
- [大量热键条目时性能] → 输入过滤在 UI 线程进行，超过 500 条目可能卡顿。→ 限制匹配数量 50 条，使用 StartWith 而非 Contains。
