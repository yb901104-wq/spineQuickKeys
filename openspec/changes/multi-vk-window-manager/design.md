## Context

当前 VK 窗口是单例架构：MainForm 持有 1 个 `VirtualButtonManager`、1 个 `VirtualLayoutSerializer`、1 个 `VirtualKeyWindow`。所有按钮和布局只有一个实例。

## Goals / Non-Goals

**Goals:**
- 支持任意数量的独立 VK 窗口，每个窗口功能与当前单窗口完全一致
- 新增 VkWindowManager 管理窗口，独立于 VK 窗口展示
- 主界面"开启/关闭"控制所有 enabled 窗口的批量显示/隐藏
- 旧版单窗口布局文件自动迁移

**Non-Goals:**
- 不改动 `VirtualButtonWidget` 渲染
- 不改动按钮拖拽排序逻辑
- 不改动 VK 窗口右键菜单除「删除当前窗口」外的其他项
- 不改动循环执行器/播放器

## Decisions

### 1. 布局文件格式

旧格式（当前）:
```json
{ "windowX": 100, "windowY": 200, ..., "buttons": [...] }
```

新格式:
```json
{
  "windows": [
    {
      "name": "窗口1",
      "windowX": 100, ...
      "enabled": true,
      "buttons": [...]
    }
  ]
}
```

迁移策略：`Load()` 时检测 — 若 JSON 根对象有 `windows` 数组则为新格式；否则视为单窗口旧格式，自动包装为 `windows[0]`。

### 2. VirtualKeyWindow 解耦

当前构造函数：
```csharp
new VirtualKeyWindow(btnManager, bindingManager, loopExecutor, serializer, sequences, callback)
```

改为：
```csharp
new VirtualKeyWindow(WindowLayoutData data, List<MacroSequence> sequences, Action? callback)
```

窗口内部自建 `VirtualButtonManager` 和 `VirtualLayoutSerializer`（只读写自己的数据块）。每个窗口的 `SaveSelf()` 将当前状态写回全局布局文件的自己那份数据。

### 3. MainForm 管理

```csharp
private List<VirtualKeyWindow> _vkWindows = [];
private BindingList<WindowLayoutData> _windowConfigs;
```

- `_windowConfigs` 从 `VirtualLayoutSerializer.Load()` 加载全部窗口配置
- 每次"开启"时遍历 `_windowConfigs` 中 `enabled=true` 的，已隐藏则创建/显示
- "关闭"时遍历所有窗口实例 `.Hide()`
- `SyncVkButtonBindings` 遍历所有窗口实例的按钮

### 4. VkWindowManager 布局

```
┌─ VkWindowManager ────────────────────────────┐
│  虚拟按键管理                              [×]│
│                                              │
│  ☑ 窗口1  │ 目标: Spine  │ 按钮: 5  │ [隐藏]│
│  ☐ 窗口2  │ 目标: 无     │ 按钮: 0  │ [显示]│
│                                              │
│  [+ 添加新窗口]    [关闭]                      │
└──────────────────────────────────────────────┘
```

DataGridView 或 ListView 展示，每一行对应一个窗口配置。

## Risks / Trade-offs

- **[迁移兼容性]** 旧布局文件无 `windows` 根键 → 加载时正确检测并包装 → 保存时写新格式（自动升级）
- **[按钮名称冲突]** 多窗口间按钮重名时 `SyncVkButtonBindings` 可能匹配到错误按钮 → 按窗口范围限定匹配，或要求名称全局唯一
- **[窗口实例状态同步]** 管理器增删窗口时需同时更新配置列表和窗口实例列表 → 用配置列表作为唯一数据源
