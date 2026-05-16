## Context

虚拟按键→目标窗口的播放链路由多个环节串联：绑定解析 → 目标窗口解析 → 方案选择 → 按键注入。其中任何一个环节静默失败都无法从日志中追溯到根因。现有日志分散、上下文不完整，缺少点击→绑定→目标→播放的完整链路追踪。

## Goals / Non-Goals

**Goals:**
- 每个按钮点击产生一条完整链路日志：按钮名 → BindActionId → 序列查找结果 → 目标窗口 → 播放方案
- 绑定同步时记录匹配统计
- 仅加 `OperationLogger` 调用，不引入新依赖

**Non-Goals:**
- 不改任何功能逻辑
- 不加第三方日志库
- 不引入性能开销（日志只在关键路径上，不含高频循环内）

## Decisions

### 日志标记格式

每行日志前缀使用 `[DIAG]` 标签，便于快速 `grep` 过滤：

```
grep "\[DIAG\]" %APPDATA%\KeyMacro\logs\yyyy-MM-dd.log
```

### 日志点分布

```
OnButtonClicked(vbtn)
  → [DIAG] VKClick: button="{name}" bindActionId="{id}" isPlaying={bool} hasTarget={bool}

  ResolveBinding(vbtn, sequences)
  找到 → [DIAG] VKBinding: button="{name}" bindActionId="{id}" -> seq="{seqName}" ({seqId})
  未找 → [DIAG] VKBinding: button="{name}" bindActionId="{id}" -> NOT_FOUND (sequences={count})

  ResolveTargetWindow()
  有目标进程 → [DIAG] VKTarget: proc="{procName}" title="{title}" -> {hwndCount} matching windows
  无目标进程 → [DIAG] VKTarget: proc="{procName}" title="{title}" -> process not running (返回0)
  无目标     → [DIAG] VKTarget: no target configured

  播放方案选择
  无目标     → [DIAG] VKPlay: scheme=DirectPlay (no target)
  目标就是前台 → [DIAG] VKPlay: scheme=DirectPlay (target is foreground)
  Scheme A   → [DIAG] VKPlay: scheme=PostMessage hwnd=0x...
  Scheme B   → [DIAG] VKPlay: scheme=ActivateWindow hwnd=0x...

SyncVkButtonBindings()
  → [DIAG] VKSync: matched {N} buttons across {M} sequences
```

## Risks / Trade-offs

- [日志量] 每个按钮点击产生约 5 条日志，正常使用不会有性能影响
