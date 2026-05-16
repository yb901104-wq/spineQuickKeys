## Why

虚拟按键捕获目标窗口后发送快捷键的功能失效，root cause 不确定。关键是几条关键路径上缺少充分日志，无法判断是绑定未命中、目标窗口解析失败、还是播放引擎被阻断。加日志后可以在用户正常使用中收集线索，快速定位问题。

## What Changes

- `VirtualKeyWindow.OnButtonClicked`：记录点击的按钮名、BindActionId、IsPlaying、目标窗口解析结果、选中的播放路径（直接 Play / Scheme A PostMessage / Scheme B 自动激活）
- `VirtualKeyWindow.ResolveTargetWindow`：记录匹配到的进程和窗口句柄数
- `VirtualKeyBindingManager.ResolveBinding`：记录 BindActionId 和查找结果
- `MainForm.SyncVkButtonBindings`：记录绑定匹配的按钮数和序列数
- `MacroPlayer.Play/PlayToWindow`：增加序列名、目标 HWND、执行步数等关键上下文

仅加日志，不改变功能逻辑。

## Capabilities

### New Capabilities
- `diagnostic-logging`: 在虚拟按键→目标窗口播放链路上增加关键节点日志

### Modified Capabilities

无（不改需求）。

## Impact

- `VirtualKeyWindow.cs`：OnButtonClicked、ResolveTargetWindow
- `VirtualKeyBindingManager.cs`：ResolveBinding
- `MainForm.cs`：SyncVkButtonBindings
- `MacroPlayer.cs`：Play、PlayToWindow
