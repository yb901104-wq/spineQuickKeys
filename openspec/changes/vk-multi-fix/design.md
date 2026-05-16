## Context

六个问题分布在 VirtualKeyWindow、VkWindowManager、MainForm、SequenceEditor 等多个模块。

## Goals / Non-Goals

**Goals:**
- 所有窗口自动补上默认皮肤
- 跨窗口按钮名称唯一
- 序列编辑中可清除 VK 绑定和热键
- 步骤和序列支持复制
- 默认值合理化
- 显示/隐藏按钮状态准确

**Non-Goals:**
- 不改绑定模型（仍以 Name 为匹配 key）
- 不做大范围重构

## Decisions

### 1. 皮肤加载：ApplyDefaultSkin 放宽判断
原条件 `string.IsNullOrEmpty(w.SkinPath)` 已覆盖 null 和 ""。重点检查反序列化时空字符串是否进来。

### 2. 按钮名唯一：增改时全窗口扫描
`VirtualButtonManager.AddButton` 和改名时扫描所有 VK 窗口（通过主窗口的 `_vkWindows` 列表），有重名则拒绝。

### 3. 解绑：编辑序列 TriggerVkButtonName 清空时同步清除
`SequenceEditor` 保存时检测 `TriggerVkButtonName` 被清空 → 找到对应 vbtn 清除 `BindActionId` → `SaveLayout()`。
热键增加一个"清除"按钮，`_txtHotkey` 置空即可，无需替换。

### 4. 复制：DataGridView 行级别复制
编辑序列步骤列表加一列按钮，点击复制当前行并插入到下一行。
主窗口加一个复制按钮，读取当前选中序列的完整数据，插入新行（名称加后缀"_副本"）。

### 5. 默认值
- `MacroStep`：`PressMode` 默认值改为 `PressMode.Point`（现有 `PressMode.Instant`？需要查模型）
- `MacroSequence.LoopIntervalMs`：默认值 200 → 100

### 6. 显示/隐藏状态
`VkWindowManager.RefreshList` 中遍历 `_vkWindows`，对比窗口隐藏状态来设置按钮文字。
