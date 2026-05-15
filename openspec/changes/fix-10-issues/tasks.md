## 1. 模型层：Spacer 支持 + 排序方法

- [x] 1.1 [VirtualButton.cs](KeyMacro/Models/VirtualButton.cs): 新增 `bool IsSpacer` 属性，默认 false
- [x] 1.2 [VirtualButtonManager.cs](KeyMacro/Services/VirtualButtonManager.cs): 新增 `MoveButton(string id, int newIndex)` 方法
- [x] 1.3 [VirtualButtonManager.cs](KeyMacro/Services/VirtualButtonManager.cs): 新增 `AddSpacerAfter(string afterId)` 方法

## 2. Widget 渲染修复（P1、P2、P5、P9 Spacer 绘制）

- [x] 2.1 [VirtualButtonWidget.cs](KeyMacro/Forms/VirtualButtonWidget.cs): P1 — 移除 ChromaKey 依赖（不再依赖 OS 透明），g.Clear(Color.Transparent) + 保证 PNG alpha 正确
- [x] 2.2 [VirtualButtonWidget.cs](KeyMacro/Forms/VirtualButtonWidget.cs): P2 — `_txtLoopCount.BackColor` 在 `ApplySkin()` 中设为 `_colorBarTop` 或 `_colorBarBottom`
- [x] 2.3 [VirtualButtonWidget.cs](KeyMacro/Forms/VirtualButtonWidget.cs): P5 — 移除 iconChar（首字大图标），名称字号加大（small: 9, large: 10, loop: 9），颜色用白色/btn_text，调整文字布局区域
- [x] 2.4 [VirtualButtonWidget.cs](KeyMacro/Forms/VirtualButtonWidget.cs): P9 — Spacer 特殊绘制（不响应点击，显示为空白分隔或小分隔线），重写 OnPaint 跳过常规绘制
- [x] 2.5 [VirtualButtonWidget.cs](KeyMacro/Forms/VirtualButtonWidget.cs): Spacer 不创建 `_txtLoopCount`，不注册鼠标事件

## 3. VK 窗口修复（P1、P4、P6、P7、P8、P9）

- [x] 3.1 [VirtualKeyWindow.cs](KeyMacro/Forms/VirtualKeyWindow.cs): P1 — 移除 TransparencyKey = ChromaKey，背景图直接绘制到 panel（已有 Panel_PaintBg），按钮 widget 不依赖 OS 透明
- [x] 3.2 [VirtualKeyWindow.cs](KeyMacro/Forms/VirtualKeyWindow.cs): P4 — 添加 Shown 事件，if _widgets.Count > 0 则 RecalculateSize()
- [x] 3.3 [VirtualKeyWindow.cs](KeyMacro/Forms/VirtualKeyWindow.cs): P6 — RebuildWidgets 末尾 `_panel.ResumeLayout()` 后加 `_panel.Invalidate()`
- [x] 3.4 [VirtualKeyWindow.cs](KeyMacro/Forms/VirtualKeyWindow.cs): P7 — 拖拽排序：在 OnButtonDragged 中检测拖拽目标索引，鼠标松开时调用 MoveButton 并 RebuildWidgets()
- [x] 3.5 [VirtualKeyWindow.cs](KeyMacro/Forms/VirtualKeyWindow.cs): P8 — 从 BuildBlankMenu() 移除"保存布局"和"重置布局"菜单项及其事件
- [x] 3.6 [VirtualKeyWindow.cs](KeyMacro/Forms/VirtualKeyWindow.cs): P8 — 检查 `m.Opened` 事件中的索引引用是否需要更新
- [x] 3.7 [VirtualKeyWindow.cs](KeyMacro/Forms/VirtualKeyWindow.cs): P9 — OnWidgetContextMenu 新增"增加间隔"菜单项，调用 AddSpacerAfter
- [x] 3.8 [VirtualKeyWindow.cs](KeyMacro/Forms/VirtualKeyWindow.cs): P9 — RecalculateSize 中，spacer 用 BASE_SPACER_W = 20 宽度（* ScaleFactor）

## 4. 主窗口修复（P3、P10）

- [x] 4.1 [MainForm.cs](KeyMacro/Forms/MainForm.cs): P3 — 将 DGV 最后一列（清除按钮）的 AutoSizeMode 改为 Fill，调整其他列宽
- [x] 4.2 [MainForm.cs](KeyMacro/Forms/MainForm.cs): P10 — 在工具栏新增"导入"和"导出"按钮，绑定事件

## 5. 导入导出系统（P10）

- [x] 5.1 新文件 [DataBundle.cs](KeyMacro/Models/DataBundle.cs): 定义统一导入导出数据模型
- [x] 5.2 新文件 [DataBundleService.cs](KeyMacro/Services/DataBundleService.cs): 实现 Export(path, bundle) 和 Import(path) 方法
- [x] 5.3 [MainForm.cs](KeyMacro/Forms/MainForm.cs): 实现导出逻辑 — 收集数据 → SaveFileDialog → 写入
- [x] 5.4 [MainForm.cs](KeyMacro/Forms/MainForm.cs): 实现导入逻辑 — OpenFileDialog → 解析 → 逐项确认 → 应用所选部分
- [x] 5.5 [SpineHotkeyEditor.cs](KeyMacro/Forms/SpineHotkeyEditor.cs): 增加接受 `string filePath, List<SpineHotkeyEntry> data` 的构造重载，导入模式下直接填充数据

## 6. 验证与发布

- [x] 6.1 `dotnet build` 确认编译通过（0 错误 0 警告）
- [ ] 6.2 启动应用，逐项测试 P1-P10 全部问题（待用户验证）
- [x] 6.3 迭代版本号 V1.99 + 更新 CLAUDE.md
- [ ] 6.4 `dotnet publish` 导出单文件 exe（待用户同意）
