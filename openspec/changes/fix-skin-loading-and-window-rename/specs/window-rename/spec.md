## ADDED Requirements

### Requirement: VkWindowManager 支持编辑窗口名称
VkWindowManager 中"窗口名称"列 SHALL 设为可编辑（`ReadOnly = false`）。
修改后 SHALL 自动持久化到 `virtual_layout.json`。

#### Scenario: 双击编辑窗口名称
- **WHEN** 用户在 VkWindowManager 中双击某行的"窗口名称"单元格
- **THEN** 单元格进入编辑状态
- **WHEN** 用户输入新名称并按 Enter 或移出焦点
- **THEN** `virtual_layout.json` 中对应窗口的 `Name` 字段更新
- **THEN** 对应 `VirtualKeyWindow` 的标题栏更新为新名称
- **THEN** 列表刷新显示新名称

#### Scenario: 窗口名称不能为空（由 DataGridView 内置行为保障）
- **WHEN** 用户清空窗口名称
- **THEN** 系统不接受空值，保持原名

### Requirement: 窗口名称作为关联 key 的同步更新
当前代码以 `Name` 作为行 `Tag` 标识符，改名后 MUST 同步更新 `DataGridViewRow.Tag`。
改名后 MUST 调用 `RefreshList()` 重建列表确保一致性。

#### Scenario: 改名后显示/删除仍能正确定位
- **WHEN** 用户将窗口 "窗口1" 重命名为 "主窗口"
- **THEN** 点击该行的"显示/隐藏"或"×"按钮
- **THEN** 操作正确作用于改名后的窗口
