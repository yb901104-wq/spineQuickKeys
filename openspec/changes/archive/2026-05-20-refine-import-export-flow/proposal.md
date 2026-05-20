## Why

当前导入导出存在三个问题：
1. **Spine 热键** 导出依赖编辑器打开，应直接从文件读；导入时覆盖写入而不是对位替换
2. **虚拟按键** 导出只取了第一个窗口，遗漏多窗口数据
3. **导入确认** 粒度太粗（4 段），应拆为更细的逐项确认，并对窗口重名做出提示

## What Changes

- DataBundle 模型：`VkData` 改为 `VkDataList`（全部窗口），字段名兼容旧格式
- 导出：SpineHotkeys 从 `ConfigService.LoadSpinePath()` 直读，不依赖编辑器
- 导出：VkData 改为导出全部窗口
- 导入：Spine 热键按 key 对位替换 `:` 后内容，不增删行
- 导入：翻译文件按 key 对位替换 `=` 后内容
- 导入：逐项确认拆为 5 项（快捷键/功能说明/序列/逐个窗口/重名提示）

## Capabilities

### New Capabilities
- `import-export-refine`: 导入导出精细化管理

### Modified Capabilities
无

## Impact

- `DataBundle.cs` — 模型字段变更
- `DataBundleService.cs` — 序列化不变（JSON 自然适配）
- `MainForm.cs` — 导出/导入方法重写
- `MacroSequence.cs` — 无改动
- `SpineHotkeyService.cs` — 无改动
