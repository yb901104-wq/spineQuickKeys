## Context

DataBundle 当前只支持单窗口 VkData，SpineHotkey 依赖编辑器窗口存在。

## Goals / Non-Goals

**Goals:**
- 导出全部 VK 窗口
- SpineHotkey 导出不依赖编辑器
- 导入时按 key 对位替换，不破坏原始文件结构
- 导入确认细化到每项/每窗口
- 窗口重名时提示

**Non-Goals:**
- 不改动 MacroPlayer、虚拟按键运行时、热键注册等非导入导出流程

## Decisions

**Export changes in `ExportDataBundle()`:**
```
VkData (single window)
  ↓
VkDataList = LoadAll().Windows.ToList()
```

```
SpineHotkeys = editor?.GetCurrentEntries() ?? null
  ↓
var spinePath = ConfigService.LoadSpinePath();
if (!string.IsNullOrEmpty(spinePath) && File.Exists(spinePath))
    SpineHotkeys = new SpineHotkeyService(spinePath).Load();
```

**Import changes in `ImportDataBundle()`:**

Legacy compatibility: read old single-window `VkData` and migrate to `VkDataList` on import.

Spine hotkey import: read target TXT → parse lines → for each line with `:`, look up imported data by name → replace keys value only → write back.

Translation import: read translations file → for each line with `=`, look up imported data by name → replace note value → write back.

Window import: iterate VkDataList, for each window check name collision, prompt user individually.

```
导入流程:
├── ① Spine 快捷键 (对位替换 `:` 后内容)   → 确认 → 写入 TXT
├── ② 按键功能说明 (对位替换 `=` 后内容)    → 确认 → 写入 translations.txt
├── ③ 宏序列                               → 确认 → 替换内存列表
└── ④ 逐个窗口:
    ├── 检查与现有窗口是否重名 → 重名则提示
    ├── 确认是否导入此窗口
    └── 是 → 添加到全局布局
```

## Risks / Trade-offs

- **[低]** 旧格式 `.kmp` 文件用 `VkData`（单窗口），新代码需要兼容读取
- **[低]** JSON 反序列化时 `VkDataList` 为 null → 尝试从 `VkData` 迁移
