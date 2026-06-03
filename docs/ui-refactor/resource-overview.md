# UI 美术资源概览

本文用于指导后续真实 UI 资源制作与替换。功能、按钮、菜单项、表格列和事件绑定必须以 `KeyMacro/Forms` 现有代码为准；本文只定义美术资源和布局替换范围。

## 基本原则

- 不用概览图新增、删除或合并功能入口。
- 默认尺寸参考 `mockups/*.png`，但真实控件数量以代码为准。
- 后续任何布局/尺寸/样式调整前，必须先核对 `docs/ui-refactor/runtime-function-map.md`，确保对应按钮、列表、输入框和菜单功能已连通。
- UI 实现顺序固定为：先对齐真实功能和控件清单，再调整组件尺寸/位置，最后调整视觉样式和资源贴图。
- 资源优先服务可读性：列表、输入框、按钮、菜单必须能明显区分。
- 优先用 WinForms 统一绘制/主题代码实现通用控件外观；仅在确实需要状态图时制作 PNG。
- `VirtualKeyWindow` 浮窗本体和 `KeyMacro/skins/*` 暂不替换，避免破坏现有 SKIN 逻辑。

## 建议资源结构

后续进入实现时，建议新增独立资源目录，避免混入 VK 皮肤目录：

```text
KeyMacro/
  assets/
    ui/
      buttons/
      panels/
      inputs/
      progress/
      menus/
      dialogs/
      icons/
```

如实际实现选择纯 GDI+ 绘制，可保留该目录只放图标、特殊纹理和文档样张。

## 已制作资源包

第一批资源已落地到 `KeyMacro/assets/ui/`，并通过 `KeyMacro/KeyMacro.csproj` 作为嵌入资源纳入项目，但尚未接入任何真实窗口逻辑。

生成器路径：`docs/ui-refactor/tools/ResourceGenerator/`

预览图路径：`docs/ui-refactor/resource-preview.png`

当前资源范围：

- `buttons/`：普通、主操作、危险、成功、工具、Spine、CLI 七类按钮，每类包含 `normal / hover / pressed / active / disabled`。
- `inputs/`：输入框、聚焦输入框、只读/禁用输入框、下拉框、搜索框。
- `panels/`：普通面板、带标题面板、工具栏底座。
- `lists/`：列表容器、表头、普通/交替/选中/悬停行。
- `tabs/`：活动与非活动 Tab。
- `progress/`：idle、running、complete、error 四类进度条。
- `checks/`：勾选框选中、未选中、禁用态。
- `menus/`：右键菜单外框、普通/悬停/危险菜单项、分隔线。
- `dialogs/`：弹窗外框、警告/错误/成功提示条。
- `titlebar/`：最小化、最大化、关闭按钮的 normal / hover / pressed 状态。
- `icons/`：常用操作和模块入口小图标，含普通与 active 色彩版本。

这些 PNG 均不包含真实按钮文字；真实文字、按钮语义和事件绑定仍以 `KeyMacro/Forms` 代码为准。

## 通用控件资源

| 资源类别 | 建议形式 | 状态 | 用途 | 备注 |
| --- | --- | --- | --- | --- |
| 按钮底座 | 代码绘制或 9-slice PNG | normal / hover / pressed / disabled | 所有普通按钮 | 深色圆角外框，内部按钮区更亮 |
| 主操作按钮 | 代码绘制 | normal / hover / pressed / disabled | 保存、确认、导出、开始执行 | 蓝色强调，不改变按钮文本 |
| 危险按钮 | 代码绘制 | normal / hover / pressed / disabled | 删除、删除全部、取消全部、释放 | 红色或红边警示 |
| 勾选框 | 代码绘制 | unchecked / checked / disabled | 启用、实验功能、后缀处理、导出配置 | 需要和按钮区分 |
| 输入框 | 代码绘制 | normal / focused / readonly / disabled | 路径、搜索、名称、参数 | 文本垂直居中，边界明显 |
| 下拉框 | 代码绘制 | normal / focused / opened / disabled | ComboBox 类选项 | 右侧箭头区独立绘制 |
| 表格/列表 | 代码绘制样式 | normal / selected / disabled | DataGridView、ListView、ListBox | 表头、选中行、网格线统一 |
| Tab | 代码绘制样式 | inactive / active | CLI、ReNameTool | 活动 Tab 使用蓝色细线 |
| 进度条 | `TextProgressBar` 样式 | idle / running / complete / error | 批量复制、CLI、ReNameTool | 当前文件文字在上方，进度在条内 |

## 窗口级资源

| 窗口 | 默认尺寸参考 | 资源重点 | 功能核准注意 |
| --- | --- | --- | --- |
| MainForm | `01-main-window.png` | 工具栏、主表格、状态栏 | 保留真实按钮：开启/关闭/管理虚拟按键均独立存在 |
| SequenceEditor | `02-sequence-editor.png` | 顶部表单、步骤工具栏、步骤表格 | 保留真实一个“添加步骤”按钮，类型在表格列选择 |
| HotkeyRecorderForm | `03-hotkey-recorder.png` | 居中录制态、按键显示框 | 不改变录制规则 |
| SpineHotkeyEditor | `04-spine-hotkey-editor.png` | 路径区、搜索、三列表格 | 保留真实列：快捷键名称、快捷键、功能说明 |
| VkWindowManager | `05-vk-manager.png` | 表格、底部新增/关闭按钮 | 新增和关闭在底部，保存即时逻辑不加按钮 |
| BatchCopyWindow | `06-batch-copy.png` | 源列表、三段路径、预览、进度 | 保留浏览前缀、清理历史等真实入口 |
| SourceFilePicker | `07-source-file-picker.png` | 缩略图网格、选择态 | 系统文件/目录选择仍原生 |
| ConflictDialog | `08-conflict-dialog.png` | 警示条、冲突列表、危险按钮 | 保留覆盖/跳过/取消全部/打开文件夹 |
| BatchCliWindow | `09/10-cli-*.png` | 双 Tab、双列表、配置选项、进度 | 保留取消 CLI、实验功能、导出配置选项 |
| ReNameTool | `12/13/14-rename-tool-*.png` | 三个 Tab、大列表、配置区、进度 | 保留真实按钮和 checkbox |
| InputDialog | `15-input-dialog.png` | 输入框、确认取消 | 可复用到 VK 菜单输入 |
| SubfolderSelectDialog | `16-subfolder-select.png` | 搜索过滤、勾选列表、底部按钮 | 保留全选/全不选/反选 |

## 菜单与弹窗资源

| 资源类别 | 覆盖对象 | 建议形式 | 注意 |
| --- | --- | --- | --- |
| ContextMenuStrip 主题 | 托盘菜单、VK 右键菜单 | 代码绘制 renderer | 菜单项内容以代码为准 |
| 核心确认弹窗 | 删除、导入、错误、成功 | 自定义 Form 或统一 helper | 替代 MessageBox 前需逐个核准按钮语义 |
| VK 菜单输入弹窗 | 改名、循环延迟、间距、缩放 | `InputDialog` 风格 | 不改 `VirtualKeyWindow` 本体和 SKIN |
| 警告条 | 冲突、危险确认、实验功能 | 代码绘制 | 不能只靠颜色表达危险 |

## 图标资源

图标不是第一优先级；第一阶段先保证布局和控件边界清楚。后续可补充小图标：

| 图标 | 用途 | 状态 |
| --- | --- | --- |
| add/edit/delete/delete-all | 主窗口和列表操作 | normal / disabled |
| copy/pause/release | 序列操作 | normal / disabled |
| spine/vk/cli/batch | 模块入口 | normal / disabled |
| folder/file/search/refresh | 文件路径与列表工具 | normal / disabled |
| warning/success/error | 弹窗提示 | static |

## 明确排除

- 不替换 `KeyMacro/skins/SpineSkin/*`。
- 不重绘 `VirtualButtonWidget` 的按钮资源。
- 不改变 `VirtualKeyWindow` 的窗口尺寸算法、缩放算法、方向算法和按钮状态逻辑。
- 不自定义系统 `OpenFileDialog`、`SaveFileDialog`、`FolderBrowserDialog`。

## 制作顺序

1. 公共主题 token：颜色、字体、边框、圆角、控件高度。
2. 通用按钮、输入框、表格、Tab、进度条绘制。
3. MainForm 与 SequenceEditor，先覆盖最高频窗口。
4. BatchCopyWindow、BatchCliWindow、ReNameTool 批量工具。
5. SpineHotkeyEditor、VkWindowManager、SourceFilePicker、SubfolderSelectDialog。
6. 自定义菜单与核心弹窗。
7. 图标和额外 PNG 状态图。

## 冲突处理

发现以下任一情况，必须暂停实现并让用户确认：

- 概览图比代码多了功能入口。
- 概览图比代码少了功能入口。
- 按钮名称和代码名称不一致，且无法判断是否只是简称。
- 多个真实按钮在概览图中被合并。
- 一个真实按钮在概览图中被拆成多个入口。
- 默认尺寸按图调整后会导致真实控件隐藏或不可操作。
