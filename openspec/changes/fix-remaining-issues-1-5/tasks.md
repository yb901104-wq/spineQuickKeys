## 1. 问题 1 - SequenceEditor DPI 全面化

- [x] 1.1 `ApplyDpiScale()` 扩展：DataGridView 列宽、步骤工具栏高度、底部面板高度乘以 DPI 系数
- [x] 1.2 将 `_suggestionListBox` 的 Width/Height/ItemHeight/Font 尺寸用 DPI 系数缩放
- [x] 1.3 将 hotkeyPanel 内按钮固定宽度（100）用 DPI 系数缩放
- [x] 1.4 将 `_txtName`/`_txtHotkey`/`_txtVkBind` 等字号用 DPI 系数缩放
- [x] 1.5 构建验证

## 2. 问题 2 - ConfigService 包装 + Spine 路径持久化

- [x] 2.1 新增 `AppConfig` 类（含 `LastSpineFilePath`、`Sequences`）
- [x] 2.2 修改 `ConfigService.Load()`：尝试读新格式，失败回退旧格式
- [x] 2.3 修改 `ConfigService.Save()`：写新格式
- [x] 2.4 修改 `MainForm.OpenSpineEditor()`：保存路径到 config；下次打开时若路径有效则跳过文件对话框
- [x] 2.5 在主窗口打开时复用 `_config` 调用 `Load()` 获取 `LastSpineFilePath`

## 3. 问题 3 - 虚拟按钮字号按比例

- [x] 3.1 在 `VirtualButtonWidget.DrawContent()` 中新增比例计算：`Height * 0.17`
- [x] 3.2 修改 `DrawContent()` 中所有字体创建，用比例字号取代 `Scaled(9)`/`Scaled(10)`
- [x] 3.3 构建验证

## 4. 问题 4 - 移除 VK 右键菜单「绑定快捷键」

- [x] 4.1 从 `OnWidgetContextMenu()` 移除 `bindItem` 相关代码（"绑定快捷键"及其子菜单"设置绑定"/"清除绑定"）
- [x] 4.2 清理 `ShowBindingDialog` 方法（不再被菜单引用，已删除）
- [x] 4.3 确认 `SyncVkButtonBindings()` 正常工作，不受移除影响
- [x] 4.4 构建验证

## 5. 问题 5 - 主窗口列宽自适应

- [x] 5.1 在 `MainForm` 中新增 `ResizeGridColumns()` 方法，按比例分配列宽
- [x] 5.2 在 `_dgv.Resize` 事件和 `RefreshGrid()` 中调用该方法
- [x] 5.3 构建验证

## 6. 构建与发布

- [x] 6.1 完整构建，修复编译错误
- [ ] 6.2 发布 exe
