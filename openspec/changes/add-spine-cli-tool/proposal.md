## Why

Spine 动画师需要批量处理导出、合并骨骼动画等重复性工作，手动操作 Spine GUI 耗时且易错。通过封装 Spine CLI，在 KeyMacro 中提供图形化批量操作界面。

## What Changes

- 新增 `BatchCliWindow` 窗口（TabControl 分两页），主窗口增加"CLI批量合并/导出"按钮
- 新增 `SpineCliService` 封装 `Spine.com` 进程调用
- 功能1（合并）：源文件列表 ↔ 目标文件列表，按规则执行 CLI 导入合并
- 功能2（批量导出）：遍历 .spine 文件、检测 export.json、执行导出/纹理打包/版本号修改

## Capabilities

### New Capabilities
- `spine-cli-tool`: Spine CLI 批量合并与导出工具

### Modified Capabilities
无

## Impact

- 新增 `BatchCliWindow.cs` + `SpineCliService.cs` + `Models/SpineCliEntry.cs`
- 主窗口增加一个按钮
- 无现有功能改动
