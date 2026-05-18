## Context

Spine 快捷键编辑器（SpineHotkeyEditor）存在两个数据流缺陷：

1. **保存时按行号匹配**：`BtnSave_Click` 用 grid 行号索引回写 `_entries`，搜索过滤后 grid 行号与 `_entries` 索引不一致，导致快捷键按键和功能说明写到错误的条目上。
2. **annotations 格式**：当前使用 `Dictionary<string, string>`，键值对含义隐晦，手动编辑不直观。

此外，主窗口的"释放"按钮在打开 Spine 编辑器后始终不激活，因为 `OpenSpineEditor()` 缺少 `UpdateSpineReleaseButton()` 调用。

## Goals / Non-Goals

**Goals:**
- 修复保存时数据错位的 bug（grid 行 → entry 的匹配方式改为用 Name）
- annotations 格式从字典改为 JSON 数组，提升可读性
- 兼容旧版字典格式的读取
- 修复释放按钮在编辑器关闭后不激活的问题

**Non-Goals:**
- 不修改 TXT 文件本身的格式（`Name: Keys` 保持不变）
- 不修改 translations 文件的格式
- 不重构 SpineHotkeyEditor 的整体架构

## Decisions

### 1. 保存匹配方式：按 Name 回写

**决定**：在 `BtnSave_Click` 中遍历 grid 行，取每行的 Name 列值到 `_entries` 中查找匹配条目，找到后回写 Keys 和 ChineseNote。

```csharp
private void BtnSave_Click(object? sender, EventArgs e)
{
    foreach (DataGridViewRow row in _dgv.Rows)
    {
        var name = row.Cells[0].Value?.ToString();
        if (string.IsNullOrEmpty(name) || name.StartsWith("---")) continue;
        var entry = _entries.FirstOrDefault(e => e.Name == name);
        if (entry == null) continue;
        entry.Keys = row.Cells[1].Value?.ToString() ?? "";
        entry.ChineseNote = row.Cells[2].Value?.ToString();
    }
    _service.Save(_entries);
    DialogResult = DialogResult.OK;
    Close();
}
```

这样无论是否过滤搜索、无论行号如何，Name 都能稳定定位。

**替代方案考虑**：
- **给 grid 行绑 Tag**：可行但侵入性大，需在 RefreshGrid 中每行存 entry 引用，且 Tag 在过滤重建后需要维护。
- **禁用过滤时保存**：体验差，用户可能忘记清除搜索框。
- **保存前清除过滤**：会丢失过滤状态且破坏用户预期。

按 Name 查找最简洁、最可靠。

### 2. AnnotationEntry 数据模型

**决定**：在 `SpineHotkeyService.cs` 中新增内部 record，替代 `Dictionary<string, string>`。

```csharp
public record AnnotationEntry(string name, string? note);
```

LoadAnnotations 返回 `List<AnnotationEntry>`，SaveAnnotations 接收 `List<AnnotationEntry>`。

### 3. 旧格式兼容

**决定**：`LoadAnnotations` 先尝试按新格式（JSON 数组）反序列化，失败则回退按旧格式（字典）反序列化并转换为数组。

```csharp
private List<AnnotationEntry> LoadAnnotations()
{
    var json = File.ReadAllText(_annotationPath);
    // Try array format first
    var arr = JsonSerializer.Deserialize<List<AnnotationEntry>>(json);
    if (arr != null) return arr;
    // Fallback: legacy dict format
    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
    return dict?.Select(kv => new AnnotationEntry(kv.Key, kv.Value)).ToList() ?? [];
}
```

### 4. 释放按钮修复

**决定**：在 `OpenSpineEditor()` 的两个 ShowDialog 路径后各加一句 `UpdateSpineReleaseButton()`。

```csharp
// 在 return 之前
UpdateSpineReleaseButton();
```

```csharp
// 在 editorFromDlg.ShowDialog() 之后
UpdateSpineReleaseButton();
```

## Risks / Trade-offs

- **Name 冲突风险**：如果有两个同名条目，按 Name 匹配会将 grid 值写给第一个命中的条目。当前 Spine 快捷键文件中 Name 是唯一的，该风险可接受。
- **旧格式文件迁移**：用户原有的 `.annotations.json` 文件在首次加载旧格式后不会自动覆写为新格式——只有在用户点保存时才会写出新格式。这意味着长期不编辑的条目保留旧格式，首次编辑后迁移。
- **性能**：`_entries.FirstOrDefault(e => e.Name == name)` 是 O(n) 查找。`_entries` 通常 200-400 条，对保存操作的性能无影响。
