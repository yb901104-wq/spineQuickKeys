## Context

上一轮修复了 Spine 编辑器保存时按 Name 匹配的问题，但仍有两个遗留问题：

1. **已损坏的文件不自动修复**：旧版本行号索引 bug 导致一些用户 TXT 文件中出现了重复条目（同名同键），新版本加载后 `_entries` 中保留了两份，保存后依然写入两份。
2. **删除快捷键不生效**：清空按键组合后点保存，但 DataGridView 正在编辑的单元格未提交，导致读取到旧值。而且即使读到了空值，`Save()` 也写入 `Name: ` 到 TXT 文件，Spine 无法正确清除该绑定。

## Goals / Non-Goals

**Goals:**
- 加载时去重，自动清理历史残留的重复条目
- 保存时跳过空键条目，真正实现"删除"快捷键
- 保存前确保单元格编辑提交

**Non-Goals:**
- 不添加 UI 级别的删除行功能（清空按键=删除的策略不变）
- 不修改 annotations 格式（上一轮已改为 JSON 数组）

## Decisions

### 1. 加载去重：用 HashSet 按 Name 去重

在 `Load()` 中维护一个 `HashSet<string>` 记录已出现的 Name，遇到重复的跳过。

```csharp
var seen = new HashSet<string>();
// ... 创建 entry 后 ...
if (!seen.Add(entry.Name)) continue;  // 重复 → 跳过
```

**替代方案**：`Load()` 返回后 `DistinctBy(e => e.Name).ToList()`。但 `DistinctBy` 保留的是第一个元素，两种方式效果相同，用 HashSet 更直观。

**为什么不改 Save 而去改 Load**：Save 只负责输出，不改 Save 可以保证旧文件在打开时自动修复。如果改 Save 去重，已经损坏的文件需要再保存一次才能修复。

### 2. 空键跳过：Save 中跳过 Keys 为空/空白

```csharp
if (string.IsNullOrWhiteSpace(entry.Keys))
{
    if (!string.IsNullOrEmpty(entry.ChineseNote))
        annotations.Add(...);
    continue;  // 不写入 TXT
}
```

注释保留到 annotations 中，方便以后参考。

### 3. 提交编辑：BtnSave_Click 开头加 EndEdit

```csharp
_dgv.EndEdit();  // 提交任何正在编辑的单元格
```

必须在遍历 grid 行之前调用，否则修改了单元格但 Value 未更新。

## Risks / Trade-offs

- **[丢失信息]** 同名但不同快捷键绑定的条目会被去重（只保留第一个）。这种情况极罕见——同一命令在 Spine 中不可能有两个不同的快捷键绑定。
- **[空键恢复难]** 清空按键保存后，该条目不再在 TXT 中。如果要恢复，只能从 annotations 文件的反向映射找回名称（仅保留了注释），或重新手动创建。但清空快捷键本身就是一个不可逆的操作，预期如此。
