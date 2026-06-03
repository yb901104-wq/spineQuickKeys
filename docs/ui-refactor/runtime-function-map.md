# UI 重构功能对表

本文件是 UI 重构阶段的功能核准表。后续调整尺寸、位置、圆角、颜色、资源贴图前，必须先确认本表对应的真实功能不缺失、不合并、不新增。

## 核准原则

- 真实功能以 `KeyMacro/Forms` 代码为准，概览图不能新增或删除功能。
- 每个按钮必须有对应事件或明确的 DialogResult 行为。
- 每个列表/表格必须有对应数据来源、刷新逻辑或选择逻辑。
- 每个输入框必须有对应读写目标、筛选逻辑或参数用途。
- 发现概览图和真实功能不一致时，先记录并让用户确认，不直接按图改代码。
- `VirtualKeyWindow` 本体和 `VirtualButtonWidget` 暂不纳入普通 UI 替换；只核对其右键菜单和相关输入弹窗功能。

## MainForm 主窗口

| 控件 | 类型 | 实际功能 | 代码连接 | 状态 |
| --- | --- | --- | --- | --- |
| 添加 | Button | 新增宏序列 | `_btnAdd.Click -> AddSequence()` | 已连通 |
| 编辑 | Button | 编辑选中序列 | `_btnEdit.Click -> EditSequence()` | 已连通 |
| 删除 | Button | 删除选中序列 | `_btnDelete.Click -> DeleteSequence()` | 已连通 |
| 删除全部 | Button | 删除全部序列 | `_btnDeleteAll.Click -> DeleteAllSequences()` | 已连通 |
| 复制序列 | Button | 复制选中序列 | `_btnDuplicate.Click -> DuplicateSequence()` | 已连通 |
| 暂停全部/恢复全部 | Button | 切换全局暂停 | `_btnPause.Click -> TogglePause()` | 已连通 |
| Spine热键编辑 | Button | 打开 Spine 热键编辑器 | `_btnSpine.Click -> OpenSpineEditor()` | 已连通 |
| 释放 | Button | 释放 Spine 热键数据 | `_btnSpineRelease.Click -> ReleaseSpineData()` | 已连通 |
| 开启虚拟按键 | Button | 显示已启用 VK 窗口 | `_btnVkOpen.Click -> OpenVirtualKeys()` | 已连通 |
| 关闭虚拟按键 | Button | 隐藏 VK 窗口 | `_btnVkClose.Click -> CloseVirtualKeys()` | 已连通 |
| 管理虚拟按键 | Button | 打开 VK 管理器 | `_btnVkManage.Click -> OpenVkManager()` | 已连通 |
| 批量重命名/spine解包整理 | Button | 打开 ReNameTool | `_btnReName.Click -> new ReNameTool.Form1()` | 已连通 |
| 批量复制 | Button | 打开 BatchCopyWindow | `_btnBatchCopy.Click -> new BatchCopyWindow()` | 已连通 |
| CLI批量合并/导出 | Button | 打开 BatchCliWindow | `_btnCli.Click -> new BatchCliWindow()` | 已连通 |
| 导入 | Button | 导入 `.kmp` 数据包 | `_btnImport.Click -> ImportDataBundle()` | 已连通 |
| 导出 | Button | 导出 `.kmp` 数据包 | `_btnExport.Click -> ExportDataBundle()` | 已连通 |
| 主列表 | DataGridView | 显示/编辑序列启用、名称、热键、目标软件、步骤、间隔、循环、选择、清除 | `RefreshGrid()` + `CellValueChanged/CellClick/CellDoubleClick` | 已连通 |
| 主列表“选择”列 | DataGridViewButtonColumn | 选择目标程序 exe，并保存进程名/显示名 | `Dgv_CellClick` column 7 | 已连通 |
| 主列表“清除”列 | DataGridViewButtonColumn | 清空当前序列目标程序绑定 | `Dgv_CellClick` column 8 | 已连通 |
| 托盘菜单 | ContextMenuStrip | 打开主窗口、暂停全部、退出 | `SetupTray()` | 已连通 |

## SequenceEditor 序列编辑器

| 控件 | 类型 | 实际功能 | 代码连接 | 状态 |
| --- | --- | --- | --- | --- |
| 序列名称 | TextBox | 编辑 `MacroSequence.Name` | `SaveToSequence()` | 已连通 |
| 触发快捷键 | TextBox readonly | 显示键盘热键录制结果 | `BtnKeyboardRecord_Click()` 写入 | 已连通 |
| 键盘录入 | Button | 打开 HotkeyRecorderForm | `_btnKeyboardRecord.Click` | 已连通 |
| 虚拟按键 | Button | 进入 VK 拾取模式 | `_btnVkPick.Click -> BtnVkPick_Click()` | 已连通 |
| 清除 | Button | 清空触发快捷键 | 本地 `btnClearHotkey.Click` | 已连通 |
| 关联虚拟按键 | TextBox | 保存 VK 复合绑定名 | `SaveToSequence()` | 已连通 |
| 添加步骤 | Button | 添加步骤行 | `_btnAddStep.Click -> BtnAddStep_Click()` | 已连通 |
| 删除步骤 | Button | 删除选中步骤 | `_btnDelStep.Click -> BtnDelStep_Click()` | 已连通 |
| 录制按键 | Button | 录制步骤按键 | `_btnRecordKey.Click -> BtnRecordKey_Click()` | 已连通 |
| 上移/下移 | Button | 移动步骤顺序 | `MoveStep(-1/1)` | 已连通 |
| 步骤列表 | DataGridView | 编辑类型、按键/文本、延迟、触发方式、按压时长、复制 | `CellValueChanged/CellClick/EditingControlShowing` | 已连通 |
| 自动补全列表 | ListBox | Spine 热键名称建议 | `_suggestionDropDown` + `ApplySuggestion()` | 已连通 |
| 取消拾取 | Button | 退出 VK 拾取模式 | `_btnCancelPick.Click -> ExitVkPickMode()` | 已连通 |
| 取消/确定 | Button | 放弃或保存序列 | `_btnCancel/_btnOk.Click` | 已连通 |

## HotkeyRecorderForm 热键录制弹窗

| 控件 | 类型 | 实际功能 | 代码连接 | 状态 |
| --- | --- | --- | --- | --- |
| 状态文本 | Label | 显示录制提示和结果 | `KeyDown/KeyUp` 更新 | 已连通 |
| 键盘事件 | Form KeyPreview | 录制快捷键并关闭 | `HotkeyRecorderForm_KeyDown/KeyUp` | 已连通 |

## SpineHotkeyEditor

| 控件 | 类型 | 实际功能 | 代码连接 | 状态 |
| --- | --- | --- | --- | --- |
| 文件路径 | Label | 显示当前 TXT 文件路径 | 构造函数/`BtnLoad_Click` | 已连通 |
| 载入文件 | Button | 选择并载入 Spine TXT | `_btnLoad.Click -> BtnLoad_Click()` | 已连通 |
| 录制按键 | Button | 对当前表格行录制快捷键 | `_btnRecord.Click -> BtnRecord_Click()` | 已连通 |
| 搜索 | TextBox | 过滤热键列表 | `_txtSearch.TextChanged -> RefreshGrid()` | 已连通 |
| 快捷键列表 | DataGridView | 编辑快捷键名称、快捷键、功能说明 | `_dgv` + `RefreshGrid()` + edit handlers | 已连通 |
| 取消/保存 | Button | 关闭或保存 TXT/注解 | `_btnCancel/_btnSave.Click` | 已连通 |

## BatchCopyWindow 批量复制

| 控件 | 类型 | 实际功能 | 代码连接 | 状态 |
| --- | --- | --- | --- | --- |
| 选择文件 | Button | 打开 SourceFilePicker 添加源文件 | `_btnSelectFiles.Click -> BtnSelectFiles_Click()` | 已连通 |
| 移除选中 | Button | 从源文件列表移除选中项 | `_btnRemoveSelected.Click` | 已连通 |
| 清空列表 | Button | 清空源文件列表 | `_btnClearFiles.Click` | 已连通 |
| 源文件列表 | ListBox | 展示待复制源文件 | `RefreshSourceList()` | 已连通 |
| 前缀 | ComboBox | 目标路径前缀和历史 | `SelectedIndexChanged -> DebouncePreview()` | 已连通 |
| 前缀选择按钮 | Button | 选择前缀文件夹并可导入子文件夹 | `_btnBrowsePrefix.Click -> BtnBrowsePrefix_Click()` | 已连通 |
| 中间 | TextBox multiline | 用户逐行编辑目标中段 | `TextChanged -> DebouncePreview()` | 已连通 |
| 添加行/删除行 | Button | 修改中间列表文本 | `_btnAddMiddle/_btnDelMiddle.Click` | 已连通 |
| 后缀 | ComboBox | 目标路径后缀和历史 | `SelectedIndexChanged -> DebouncePreview()` | 已连通 |
| 预览列表 | ListBox | 展示拼接后的目标路径 | `UpdatePreview()` | 已连通 |
| 清理历史记录 | Button | 清空前缀/后缀历史 | `_btnClearHistory.Click` | 已连通 |
| 开始复制/取消复制 | Button | 启动或取消异步复制 | `_btnStartCopy.Click -> BtnStartCopy_Click()` | 已连通 |
| 当前文件文字/进度条 | Label + TextProgressBar | 显示批量复制进度 | `Progress` 回调更新 | 已连通 |

## SourceFilePicker 源文件选择器

| 控件 | 类型 | 实际功能 | 代码连接 | 状态 |
| --- | --- | --- | --- | --- |
| 目录输入 | TextBox readonly | 显示缩略图目录 | `BrowseDir()` 写入 | 已连通 |
| 浏览 | Button | 选择目录 | `_btnBrowse.Click -> BrowseDir()` | 已连通 |
| 刷新 | Button | 重新加载缩略图 | `_btnRefresh.Click -> LoadThumbnailsAsync()` | 已连通 |
| 缩略图列表 | ListView | 展示并勾选源文件 | `_lvThumbnails.ItemCheck` | 已连通 |
| 取消/确认选择 | Button | 关闭或返回勾选文件 | `_btnCancel/_btnOk.Click` | 已连通 |

## ConflictDialog 冲突弹窗

| 控件 | 类型 | 实际功能 | 代码连接 | 状态 |
| --- | --- | --- | --- | --- |
| 冲突列表 | ListBox | 显示同名文件 | 构造函数填充 | 已连通 |
| 取消全部复制 | Button | 返回 `CancelAll` | `btnCancelAll.Click` | 已连通 |
| 跳过冲突 | Button | 返回 `Skip` | `btnSkip.Click` | 已连通 |
| 覆盖 | Button | 返回 `Overwrite` | `btnOverwrite.Click` | 已连通 |
| 打开文件夹 | Button | Explorer 打开目标目录 | `btnOpenFolder.Click` | 已连通 |

## BatchCliWindow CLI 合并/导出

| 控件 | 类型 | 实际功能 | 代码连接 | 状态 |
| --- | --- | --- | --- | --- |
| Spine.com 路径 | TextBox | CLI 路径参数 | `LoadSavedPath/DetectSpine` | 已连通 |
| 检测/选择/取消CLI | Button | 检测、选择 CLI、取消运行中任务 | `_btnDetect/_btnBrowseSpine/_btnCancelCli.Click` | 已连通 |
| 合并源文件列表 | ListView | 展示源 `.json/.skel` 文件和动画选择 | `_lvSource` + `AddSourceFile()` | 已连通 |
| 目标文件列表 | ListView | 展示目标文件 | `_lvTarget` + `AddTargetFiles()` | 已连通 |
| 添加/删除源文件 | Button | 修改源列表 | `_btnSourceAdd/_btnSourceRemove.Click` | 已连通 |
| 动画选择 | Button | 打开动画选择弹窗 | `_btnAnimSelect.Click` | 已连通 |
| 添加/删除目标文件 | Button | 修改目标列表 | `_btnTargetAdd/_btnTargetRemove.Click` | 已连通 |
| --from / --to | TextBox | 合并命名参数 | `ExecuteMerge()` 读取 | 已连通 |
| 实验功能 | CheckBox | 切换实验合并路径 | `_chkExperimental.Checked` | 已连通 |
| 执行合并 | Button | 运行普通/实验合并 | `_btnMergeExecute.Click` | 已连通 |
| 批量导出源目录 | TextBox | 扫描导出源目录 | `_btnScan.Click -> ScanSourceDir()` | 已连通 |
| 浏览/扫描 | Button | 选择并扫描源目录 | `_btnBrowseSource/_btnScan.Click` | 已连通 |
| 导出文件列表 | ListView | 显示待导出项目和配置状态 | `_lvExportFiles` + `RefreshExportStatus()` | 已连通 |
| 刷新状态 | Button | 重新检测 export.json 状态 | `_btnRefresh.Click` | 已连通 |
| 导出配置 | RadioButton | 切换 finish/demotion/其他 export.json | `CheckedChanged` 更新 `_exportConfigName` | 已连通 |
| 输出目录 | TextBox | 导出目标目录 | `_btnBrowseOutput` 写入，导出读取 | 已连通 |
| 导出/单纹理图 | Button | 执行导出或打包 | `_btnExport/_btnPack.Click` | 已连通 |
| 当前文件文字/进度条 | Label + TextProgressBar | 显示 CLI 批量进度 | `UpdateProgress()` | 已连通 |

## AnimationSelect 动画选择弹窗

| 控件 | 类型 | 实际功能 | 代码连接 | 状态 |
| --- | --- | --- | --- | --- |
| 动画勾选列表 | CheckedListBox | 选择源文件参与合并的动画 | `ShowAnimSelectForSource()` | 已连通 |
| 取消/确认 | Button | 放弃或保存动画选择 | `btnCancel/btnOk.Click` | 已连通 |

## VkWindowManager

| 控件 | 类型 | 实际功能 | 代码连接 | 状态 |
| --- | --- | --- | --- | --- |
| VK 窗口列表 | DataGridView | 编辑窗口名称、允许显示、显示/隐藏、删除 | `RefreshList()` + `CellValueChanged/CellClick` | 已连通 |
| 新增窗口 | Button | 新增 VK 窗口数据 | `_btnAdd.Click -> AddWindow()` | 已连通 |
| 关闭 | Button | 关闭管理器 | `_btnClose.Click` | 已连通 |

## ReNameTool 批量重命名/整理/解包

| 控件 | 类型 | 实际功能 | 代码连接 | 状态 |
| --- | --- | --- | --- | --- |
| 重命名文件列表 | ListBox | 展示待重命名文件 | `button1_Click/button4_Click` 填充 | 已连通 |
| 选择文件/选择文件夹 | Button | 导入文件或文件夹文件 | `button1_Click/button4_Click` | 已连通 |
| 清空列表 | Button | 清空重命名列表 | `button2_Click` | 已连通 |
| 关键字/替换词/新名字 | TextBox | 重命名参数 | `button3_Click/button5_Click` 读取 | 已连通 |
| 局部替换/统一重命名 | Button | 执行重命名 | `button3_Click/button5_Click` | 已连通 |
| 整理源/保存路径 | TextBox | Spine 整理路径 | `button6_Click/button7_Click` 写入，`button8_Click` 读取 | 已连通 |
| .bytes及.txt后缀 | CheckBox | 整理后缀处理开关 | `button8_Click` 读取 | 已连通 |
| 开始整理/清空列表 | Button | 整理或清空 | `button8_Click/button9_Click` | 已连通 |
| 解包目标文件夹 | TextBox | 图集解包目录 | `button10_Click` 写入 | 已连通 |
| 解包列表 | ListBox | 展示 `.atlas` 文件 | `button10_Click` 填充 | 已连通 |
| 开始解包/清空列表 | Button | 解包或清空 | `button13_Click/button11_Click` | 已连通 |
| 三个进度区 | Label + TextProgressBar | 显示重命名/整理/解包进度 | `SetProgress()` | 已连通 |

## InputDialog

| 控件 | 类型 | 实际功能 | 代码连接 | 状态 |
| --- | --- | --- | --- | --- |
| 输入框 | TextBox | 返回用户输入 | `Result => _txtInput.Text` | 已连通 |
| 取消/确定 | Button | 返回 DialogResult | `btnCancel/btnOk.Click` | 已连通 |

## SubfolderSelectDialog

| 控件 | 类型 | 实际功能 | 代码连接 | 状态 |
| --- | --- | --- | --- | --- |
| 搜索/不包含 | TextBox | 过滤候选文件 | `TextChanged -> ApplyFilter()` | 已连通 |
| 勾选列表 | CheckedListBox | 选择导入项 | `_clb.ItemCheck` | 已连通 |
| 全选/全不选 | Button | 批量切换勾选 | `btnSelectAll/btnDeselectAll.Click` | 已连通 |
| 取消/确认添加 | Button | 返回选中项 | `btnCancel/btnOk.Click` | 已连通 |

## VirtualKeyWindow 菜单功能核对

| 菜单 | 实际功能 | 代码连接 | 状态 |
| --- | --- | --- | --- |
| 空白右键增加按钮/删除所有/置顶/透明度/锁定/目标窗口/方向/缩放/关闭/删除窗口 | 管理 VK 窗口和布局 | `BuildBlankMenu()` | 已连通 |
| 按钮右键修改名称/循环延迟/按钮间距/强制停止/删除当前按钮 | 管理单个按钮 | `OnWidgetContextMenu()` | 已连通 |
| VK 本体 UI/SKIN | 本轮不改 | `VirtualButtonWidget` + `VkSkinLoader` | 排除 |

## 当前发现

- 暂未发现“按钮创建但没有事件”的主要功能缺口。
- 主窗口工具栏按钮数量多，已将工具栏改为允许换行，避免默认窗口宽度不足时右侧按钮被裁切导致功能入口不可点击。
- 主题层已调整为只增高 `AutoSize` 按钮，不强行改变 Designer 固定尺寸按钮，避免 ReNameTool 这类固定坐标界面出现按钮重叠而影响点击。
- `ReNameTool` 内部是固定坐标 Designer 布局，功能连通但后续视觉重排需要单独处理，不能只靠统一主题套用。
- 后续任何组件尺寸、位置、样式修改，都必须以本功能对表为准逐项确认。
