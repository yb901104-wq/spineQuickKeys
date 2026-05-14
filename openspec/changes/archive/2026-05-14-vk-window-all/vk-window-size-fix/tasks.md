## 1. LayoutData 扩展

- [x] 1.1 VirtualLayoutSerializer.LayoutData 增加 ScaleFactor 字段 (float, default 0)

## 2. 尺寸逻辑重写（A）

- [x] 2.1 Resize 事件处理只挪缩放手柄位置，移除 UpdateScale 调用
- [x] 2.2 拖拽缩放手柄逻辑改为：MouseMove 调 UpdateScale 预览 + MouseUp 调 RecalculateSize
- [x] 2.3 RecalculateSize 单排：widget 当前尺寸 × 1.1，不读 ClientSize
- [x] 2.4 LoadLayoutData 单排：恢复 ScaleFactor + Location，不恢复 Size，然后 UpdateScale + RecalculateSize
- [x] 2.5 SaveLayout 保存 ScaleFactor
- [x] 2.6 SetScaleFromMenu 删除多余逻辑，保留 UpdateScale + RecalculateSize

## 3. 工具栏隐藏（B）

- [x] 3.1 UpdateWindowLockState 改为切换 Height (0/28) 而非 Visible
- [x] 3.2 确认面板按钮位置在锁定/解锁前后不变

## 4. 工具栏拖拽 + 缩放手柄 Z 序（C）

- [x] 4.1 _lblToolbarInfo 挂载 MouseDown 拖拽事件
- [x] 4.2 Controls 顺序改为 toolbar → panel → resizeGrip，resizeGrip.BringToFront()
