## 1. 删除缩放手柄

- [x] 1.1 删除 _resizeGrip 字段声明
- [x] 1.2 删除 resizeGrip 创建、Paint、MouseDown/Move/Up 事件代码
- [x] 1.3 删除 _isResizing、_resizeStart、_resizeStartSize 字段
- [x] 1.4 从 Controls 移除 resizeGrip，从 Resize 事件移除定位代码

## 2. 标题栏 Dock=Top

- [x] 2.1 恢复 _toolbar.Dock = DockStyle.Top
- [x] 2.2 _toolbar.Visible = true（构造时默认显示）
- [x] 2.3 UpdateWindowLockState 切换 Visible，不再改 panel padding
- [x] 2.4 删除 toolbar 浮动相关的代码（BringToFront、Resize 宽设置）

## 3. 无尺寸限制

- [x] 3.1 删除构造函数 MinimumSize
- [x] 3.2 RecalculateSize 中删除所有 Math.Max 调用

## 4. 自定义缩放菜单

- [x] 4.1 缩放子菜单增加 "自定义..."，InputBox 输入 10-200
- [x] 4.2 SetScaleFromMenu 已存在，直接调用

## 5. RecalculateSize 简化

- [x] 5.1 单排：width = padding + sum(widget.Width)；height = (toolbar.Visible ? 28 : 0) + padding + max(widget.Height)
- [x] 5.2 多排：height 计算适配 toolbar.Visible
