## Why

六个体验问题累积到了需要一次性清理的程度：多窗口皮肤不一致、按钮重名、绑定无法解除、缺少复制操作、默认值不合理、显示隐藏状态不同步。单独修任何一个都不够，放一起改效率更高。

## What Changes

1. **皮肤加载**：ApplyDefaultSkin 覆盖空字符串 SkinPath，确保旧窗口也能补上默认皮肤
2. **按钮名唯一**：增改按钮时校验当前窗口及同 VkWindowManager 下其他窗口的名称唯一性
3. **解绑 + 热键释放**：编辑序列时清空 TriggerVkButtonName 后自动解绑 BindActionId；热键增加清除按钮
4. **复制**：编辑序列每行加复制步骤按钮；主窗口加复制序列按钮
5. **默认值**：PressMode 默认 Point触按；LoopIntervalMs 默认 100
6. **显示/隐藏状态同步**：VkWindowManager.RefreshList 根据窗口实际可见状态设置按钮文字

## Capabilities

### New Capabilities
- `button-name-unique`: 虚拟按钮名称在同一 VkWindowManager 下全局唯一
- `step-sequence-duplicate`: 编辑序列支持复制步骤、主窗口支持复制序列
- `bind-clear`: 序列编辑中清除绑定 + 快捷键释放

### Modified Capabilities
- `skin-loading` (openspec/specs/skin-loading/spec.md): ApplyDefaultSkin 覆盖空字符串而非仅 null
- `widget-context-menu` (openspec/specs/widget-context-menu/spec.md): 新增"解除绑定"菜单项

## Impact

- `VirtualKeyWindow.cs`：按钮名唯一校验；清除绑定入口
- `VkWindowManager.cs`：显示/隐藏按钮状态同步
- `MainForm.cs`：复制序列按钮
- `SequenceEditor.cs`：复制步骤按钮；解绑逻辑；热键清除；默认值
- `VirtualKeyBindingManager.cs`：Unbind 调用
- `VkSkinLoader.cs`/`VirtualLayoutSerializer.cs`：ApplyDefaultSkin 覆盖空字符串
- `MacroSequence.cs`：LoopIntervalMs 默认 100
- `MacroStep.cs`：PressMode 默认 Point
