## Why

虚拟按键窗口捕获目标窗口时同时保存了进程名和窗口标题。Spine 标题包含当前项目名（如 "Spine 4.x - 项目A.skeleton"），切换项目后标题变化，精确匹配失败，导致无法找到目标窗口，需要用户重新捕获。规范已要求在标题匹配失败时 fallback 到进程名匹配，但代码未实现。

## What Changes

- 修改 `VirtualKeyWindow.ResolveTargetWindow()`：当 `_targetTitle` 设定但精确匹配失败时，fallback 到按进程名取第一个合法窗口
- 无新增 UI、无配置变更、无 spec 改动（spec 已描述正确行为，仅修复实现偏离）

## Capabilities

### New Capabilities
无

### Modified Capabilities
- `target-window-capture`: 解析逻辑改为"先按标题精确匹配，失败后按进程名回退"，与 spec 一致

## Impact

仅 `VirtualKeyWindow.cs` 中 `ResolveTargetWindow()` 方法，约 5 行改动
