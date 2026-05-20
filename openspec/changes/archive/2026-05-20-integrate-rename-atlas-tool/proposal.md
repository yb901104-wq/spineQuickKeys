## Why

有一个独立的 WinForms 工具 "ReName图集解包版本"（D:\AI_cc\spine-tool-1），包含批量重命名、Spine 文件整理、图集自动解包三个功能，与 KeyMacro 同属 Spine 工作流。目前需单独打开，操作割裂。将其整体迁入 KeyMacro，统一入口，消除多工具切换开销。

## What Changes

- 将 `ReName-UnpackingAtlas` 项目 ( .NET Framework 4.8 ) 的 Form1 整体搬迁到 KeyMacro 项目 ( .NET 9.0 ) 的 `Forms/ReNameTool/` 目录下
- MainForm 工具栏新增 "图集工具" 按钮，点击打开搬迁后的 Form1（ShowDialog）
- 不修改原有功能、UI 布局、控件尺寸/位置、业务逻辑
- 不修改 KeyMacro 现有功能

## Capabilities

### New Capabilities
- `reame-atlas-tool`: 文件批量重命名 / Spine 文件按名整理 / Spine 图集自动解包

### Modified Capabilities
无

## Impact

- KeyMacro 新增 `Forms/ReNameTool/` 目录，包含 Form1.cs + Designer.cs + resx + oubao.ico
- `KeyMacro.csproj` 需添加新文件引用（SDK 风格通常自动包含）
- MainForm 增加一个按钮和对应的 click handler
