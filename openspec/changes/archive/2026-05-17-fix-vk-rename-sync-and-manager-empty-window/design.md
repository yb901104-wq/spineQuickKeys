## Context

### Bug 1: 按钮改名后序列绑定未同步
- 改名操作只更新了 `vbtn.Name` 和 `SaveLayout()`，未更新绑定序列的 `TriggerVkButtonName`
- `BindActionId` 是运行时缓存，改名后变脏但未刷新，下次 `SyncVkButtonBindings` 会断绑
- 涉及文件：`VirtualKeyWindow.cs:221-225`

### Bug 2: 管理器"显示"打开空窗口
- `VkWindowManager.Dgv_CellClick` 传了 `new WindowLayoutData { Name = name }`，缺少 Buttons、SkinPath 等字段
- 主界面"开启虚拟按键"走 `LoadAll()` 拿完整数据则正常
- 涉及文件：`VkWindowManager.cs:185-186`

## Goals / Non-Goals

**Goals:**
- 改名后绑定序列的 `TriggerVkButtonName` 同步更新
- 改名后触发 `SyncVkButtonBindings` 刷新缓存
- 管理器"显示"按钮传递完整窗口数据

**Non-Gols:**
- 不改绑定架构（不改 `BindActionId` 设计）
- 不改布局文件格式
- 不涉及虚拟按钮删除时的序列解绑（已有其他代码处理）

## Decisions

### Bug 1 方案：同步更新 + 触发回调
- 改名后在 `_sequences` 中查找 `TriggerVkButtonName` 匹配 `"窗口名/旧名"` 或纯 `"旧名"` 的序列
- 更新为 `"窗口名/新名"` 或纯 `"新名"`
- 调用 `_sequencesChangedCallback`（即 MainForm 的 `SaveAndRefresh`，内含 ConfigService.Save + SyncVkButtonBindings）
- 不直接调 `SyncVkButtonBindings` 是为了保持回调链完整（保存、刷新主界面）

### Bug 2 方案：从 JSON 加载完整数据
- `Dgv_CellClick` 中先 `LoadAll()` 找到完整的 `WindowLayoutData`
- 传递该完整对象而非 `new WindowLayoutData { Name = name }`

## Risks / Trade-offs

- [改名同步] 如果存在多个序列绑定到同一按钮名（理论上不应当），会全部更新 — 这是正确行为
- [空窗口] 用户打开管理器时若 window 数据已被删除但管理器的显示列表未刷新，传 null 时应跳过操作。不过管理器每次 `RefreshList` 都重新 `LoadAll`，不会出现此情况