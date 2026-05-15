## 1. 主窗口列宽等比缩放

- [ ] 1.1 修改 `RefreshGrid()`：文本列改用 `AutoSizeMode = Fill` + `FillWeight`
- [ ] 1.2 固定列（启用、选择）保持 `None`，清除列保持 `Fill`
- [ ] 1.3 设置 `MinimumWidth` 防止过窄
- [ ] 1.4 构建验证

## 2. Spine 路径持久化

- [ ] 2.1 在 `ConfigService` 中新增 `SaveSpinePath()` / `LoadSpinePath()` / `ClearSpinePath()` 静态方法
- [ ] 2.2 修改 `MainForm_Shown`：启动时自动载入 spine 数据
- [ ] 2.3 修改 `OpenSpineEditor()`：持久化路径，下次跳过对话框
- [ ] 2.4 构建验证

## 3. 释放按钮

- [ ] 3.1 在 `BuildUI()` 中新增 `_btnSpineRelease` 按钮，放在 `_btnSpine` 右侧
- [ ] 3.2 实现点击事件：清除 `LastLoadedEntries` + 删除路径文件
- [ ] 3.3 在 `MainForm_Shown` 和释放时更新按钮启用状态
- [ ] 3.4 构建验证

## 4. 发布

- [ ] 4.1 完整构建，修复编译错误
- [ ] 4.2 发布 exe
