## 1. Bug 2：管理器"显示"按钮传递完整数据

- [x] 1.1 `VkWindowManager.cs` — `Dgv_CellClick` 中从 JSON 加载完整 `WindowLayoutData` 再传递

## 2. Bug 1：改名后同步序列绑定

- [x] 2.1 `VirtualKeyWindow.cs` — 改名处理中遍历 `_sequences`，更新匹配的 `TriggerVkButtonName`（复合名和纯名）
- [x] 2.2 改名后触发 `_sequencesChangedCallback` 以保存并刷新绑定缓存

## 3. 验证

- [x] 3.1 构建并确认编译通过
- [x] 3.2 运行测试确认两处修改有效
