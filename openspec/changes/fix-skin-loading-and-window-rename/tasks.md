## 1. 修复皮肤磁盘路径

- [x] 1.1 在 `VkSkinLoader` 构造函数中增加 `{CWD}/KeyMacro/skins/{name}` 路径检测
- [x] 1.2 保留原有两条路径作为 fallback
- [ ] 1.3 验证开发模式（`dotnet run --project KeyMacro`）下能从磁盘正常加载 PNG

## 2. 验证嵌入式资源编译

- [x] 2.1 编译后检查程序集中是否包含 `KeyMacro.skins.SpineSkin.*.png` 资源
- [x] 2.2 必要时修改 `.csproj` 的 EmbeddedResource 配置

## 3. 补齐 SpineSkin pressed/active 图片

- [x] 3.1 生成 `btn_small_pressed.png` / `btn_small_active.png`
- [x] 3.2 生成 `btn_large_pressed.png` / `btn_large_active.png`
- [x] 3.3 生成 `btn_loop_pressed.png` / `btn_loop_active.png`

## 4. VkWindowManager 窗口重命名

- [x] 4.1 将"窗口名称"列 `ReadOnly` 改为 `false`
- [x] 4.2 在 `CellValueChanged` 中处理 column 0，更新 `_serializer` 和 `VirtualKeyWindow._data.Name`
- [x] 4.3 改名后同步更新行 `Tag` 并调用事件通知 MainForm
- [ ] 4.4 验证改名后显示/隐藏和删除操作仍正确

## 5. 测试验证

- [ ] 5.1 `dotnet run` 确认皮肤 PNG 正常加载
- [ ] 5.2 确认 pressed/active 状态显示正确
- [ ] 5.3 确认窗口重命名后持久化到 json 文件
