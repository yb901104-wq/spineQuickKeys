## 1. 重写 VirtualKeyWindow.cs

- [x] 1.1 编写新文件 VirtualKeyWindow.cs（保留类名、public 接口、字段声明）
- [x] 1.2 实现构造函数：FormBorderStyle=None、Size、标题栏 Dock=Top、Panel Dock=Fill
- [x] 1.3 实现标题栏：Label + 关闭按钮 + 拖拽事件（screen 坐标，排除关闭区域）
- [x] 1.4 实现 BuildBlankMenu（右键菜单）+ OnWidgetContextMenu（按钮菜单）
- [x] 1.5 实现 RecalculateSize（单排/多排，含 Form.Padding 补偿）
- [x] 1.6 实现 SaveLayout/LoadLayoutData（含屏幕边界检测）
- [x] 1.7 实现 CaptureTargetWindow/ResolveTargetWindow（目标窗口捕获）
- [x] 1.8 实现 OnButtonClicked（VkPickMode/Loop/方案A/方案B/无目标）
- [x] 1.9 实现 UpdateScale/RebuildWidgets/布局模式切换
- [x] 1.10 实现 ToggleWindowLock/UpdateWindowLockState
- [x] 1.11 编译通过，验证功能完整
