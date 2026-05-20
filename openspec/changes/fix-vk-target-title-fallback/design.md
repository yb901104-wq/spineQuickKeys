## Context

`VirtualKeyWindow.ResolveTargetWindow()` 当前按先进程名再窗口标题精确匹配查找目标窗口句柄。当标题设定且不匹配时，会 `continue` 跳到下一进程（若无则返回 `IntPtr.Zero`）。Spine 等应用的窗口标题随打开项目变化，导致换项目后匹配失败。

## Goals / Non-Goals

**Goals:**
- 标题精确匹配失败时自动按进程名回退，找到目标窗口
- 零配置变更
- 保留标题匹配用于多实例消歧的潜在用途

**Non-Goals:**
- 不改变 UI/不新增菜单项
- 不改变捕获流程
- 不处理多 Spine 实例场景（当前也不支持）

## Decisions

**Decision**: 在标题匹配循环中记录第一个合法 hwnd，精确匹配全部失败后返回该 hwnd

```
ResolveTargetWindow()
├── 遍历所有匹配进程名的进程
│   ├── 跳过无主窗口或无效句柄的进程
│   ├── 记录第一个合法 hwnd 到 firstValidHwnd
│   ├── 如果 _targetTitle 设定 → 尝试精确匹配
│   │   ├── 匹配成功 → return hwnd
│   │   └── 匹配失败 → continue（继续遍历）
│   └── 如果 _targetTitle 未设定 → return hwnd (process-only)
├── 循环结束后 firstValidHwnd 非空 → return firstValidHwnd
└── return IntPtr.Zero
```

- **为什么不是移除标题匹配**：标题匹配允许未来按需消歧（如多个 Spine 窗口），保留不破坏
- **为什么不是 `Contains`/`StartsWith` 部分匹配**：无法确定哪部分是项目名哪部分是固定前缀，部分匹配可能引入误匹配

## Risks / Trade-offs

- **[低]** 多 Spine 实例时回退可能匹配到错误实例 — 与当前未设标题时的行为一致，不引入新问题
- **[无]** 改动在单进程内，5 行，无外围影响
