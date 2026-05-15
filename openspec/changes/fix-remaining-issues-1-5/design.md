## Context

当前项目有 5 个待修复问题，覆盖 4 个文件：SequenceEditor、ConfigService、MainForm、VirtualButtonWidget、VirtualKeyWindow。各问题独立但改动的文件有交叉（如 MainForm 涉及问题 4 和 5），需要统一协调。

## Goals / Non-Goals

**Goals:**
- SequenceEditor 全部控件的 DPI 感知（问题 1）
- Spine 热键文件路径持久化（问题 2）
- 虚拟按钮字号与按钮尺寸比例固定（问题 3）
- 支持序列同时响应快捷键和虚拟按键 + 移除 VK 右键菜单冗余绑定（问题 4）
- 主窗口列宽随窗口缩放自适应（问题 5）

**Non-Goals:**
- 不涉及 VK 窗口多排/方向切换（已在 orientation-toggle 变更中处理）
- 不改动 MacroPlayer、HotkeyService 核心逻辑
- 不改动 Spine TXT 文件解析/保存机制
- 不改动导入导出功能

## Decisions

### 问题 1 - SequenceEditor DPI 全面化

当前 `ApplyDpiScale()` 只缩放 `_topPanel`。需要覆盖：
- `_dgvSteps` 列宽（90, 80, 90, 100）
- 步骤工具栏高度 38 → `Scaled(38)`
- 底部面板高度 48 → `Scaled(48)`
- `_suggestionListBox` 的 Width/Height/ItemHeight/字号
- hotkeyPanel 内按钮宽度 100 → `Scaled(100)`
- 各字体字号（`new Font(..., 10)` → `new Font(..., Scaled(10))`）

方案：在 `ApplyDpiScale()` 中统一缩放，所有硬编码尺寸调用 `(int)(val * ds)`。

### 问题 2 - ConfigService 包装为 AppConfig

当前 `config.json` 直接存 `List<MacroSequence>`。需要兼容旧格式：

方案：新增 `AppConfig` 类：
```csharp
public class AppConfig
{
    public string? LastSpineFilePath { get; set; }
    public List<MacroSequence> Sequences { get; set; } = [];
}
```

加载策略：
1. 尝试读取新格式（含 `Sequences` 和 `LastSpineFilePath`）
2. 若失败，回退读取旧格式（裸数组 `List<MacroSequence>`）
3. 保存时始终写新格式

这样向后兼容旧 `config.json`。

### 问题 3 - 字号按按钮高度比例

当前 `Scaled(9)` 在 48px 按钮上 ≈ 19%。改为按比例计算：

```
fontSize = Math.Max(6, (int)(btnHeight * 0.22))
// btnHeight = Scaled(BASE_BTN_H) = Scaled(48)
// 48 × 0.22 ≈ 10.56 → 取整 10 或 11
```

使用按钮的 `Height` 属性而非 `Scaled(48)`，让字号随着按钮实际尺寸变化。所有样式统一使用同一比例。

### 问题 4 - 移除 VK 右键「绑定快捷键」

当前状态：
- `SyncVkButtonBindings()` 通过 `TriggerVkButtonName ↔ vbtn.Name` 匹配来设 `BindActionId`
- VK 右键「绑定快捷键」直接设 `BindActionId`，与 SyncVkButtonBindings 冲突
- `MacroSequence` 的 `TriggerHotkey` 和 `TriggerVkButtonName` 独立运作，可同时生效

方案：
- 移除 `BuildBlankMenu()` 中「绑定快捷键」的 menu 构建（`bindItem` 相关代码，约 10 行）
- 保留 `VirtualKeyBindingManager`（仍被 `SyncVkButtonBindings` 在 MainForm 中调用）
- 保留 `ShowBindingDialog` 方法（以防后续需要，但菜单入口移除）
- 保留 `HasBoundButtons()` 属性

注意：`widget-context-menu` spec 需要更新，移除绑定快捷键相关的场景。

### 问题 5 - 主窗口列宽自适应

方案：保持 `AutoSizeColumnsMode = None`，但在 `Resize` 事件中按比例重算列宽。具体：
- 固定宽度列：启用列、选择、清除（窄列保持固定）
- 弹性宽度列：序列名称(30%)、触发快捷键(20%)、目标软件(20%)、间隔(15%)、循环(15%)

比 `Fill` 模式更可控，避免按钮列和窄列被压扁。

## Risks / Trade-offs

- **[问题 2 - config 兼容]** 旧 `config.json` 只有数组 → 新格式有 `Sequences` 字段。需要加载时探测旧格式并自动转换。误判可能导致序列数据丢失 → 写单元测试覆盖
- **[问题 4 - 用户习惯]** 现有用户可能依赖 VK 右键绑定快捷键。移除前确保 `SyncVkButtonBindings` 能完全覆盖其功能（已在 V2.0 中实现且正常工作）
- **[问题 5 - Fill vs 比例]** `Fill` 模式更简单但无法控制每列最小宽度。手动比例更可控但代码多一些
