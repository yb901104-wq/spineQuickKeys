## 1. VirtualLayoutSerializer 多窗口改造

- [x] 1.1 重构数据模型：`LayoutData` → `WindowLayoutData`（单窗口），新增 `GlobalLayoutData`（含 `List<WindowLayoutData>`）
- [x] 1.2 `Load()` 检测旧格式（无 `windows` 键）自动迁移为新格式
- [x] 1.3 `Save()` 始终写新格式
- [x] 1.4 添加 `WindowLayoutData.Enabled` 字段

## 2. VirtualKeyWindow 自管理改造

- [x] 2.1 构造函数改为接收 `WindowLayoutData` + `List<MacroSequence>` + callback
- [x] 2.2 窗口内部自建 `VirtualButtonManager`，从 `data.Buttons` 载入
- [x] 2.3 窗口内部自建 `VirtualKeyBindingManager` 和 `VirtualLoopExecutor`
- [x] 2.4 新增 `WindowData` 属性（返回当前窗口的 `WindowLayoutData`）
- [x] 2.5 SaveLayout 持久化整个 GlobalLayoutData 中自己的那份数据
- [x] 2.6 右键菜单「关闭窗口」保持隐藏；新增「删除当前窗口」项彻底删除

## 3. VkWindowManager 管理窗口

- [x] 3.1 新建 `VkWindowManager.cs` Form，通过事件与 MainForm 通信
- [x] 3.2 DataGridView 布局：名称 / 目标 / 按钮数 / 允许显示(checkbox) / 显示隐藏(按钮) / 删除(按钮)
- [x] 3.3 "[+ 添加新窗口]" 按钮：自动编号命名，默认 enabled
- [x] 3.4 显示/隐藏按钮：即时切换单个窗口可见性
- [x] 3.5 删除按钮：关闭+销毁窗口，移除配置数据
- [x] 3.6 允许显示 checkbox 变更时持久化到布局文件

## 4. MainForm 改造

- [x] 4.1 `_vkBtnManager`/`_vkWindow` 替换为 `List<VirtualKeyWindow>`
- [x] 4.2 "开启虚拟按键" 遍历 enabled 窗口统一显示
- [x] 4.3 "关闭虚拟按键" 隐藏所有窗口（不销毁）
- [x] 4.4 "管理虚拟按键" 按钮打开 VkWindowManager
- [x] 4.5 `SyncVkButtonBindings` 遍历所有窗口的按钮
- [x] 4.6 导入导出适配多窗口数据
- [x] 4.7 主界面按钮改为「开启/关闭/管理虚拟按键」

## 5. 构建与验证

- [x] 5.1 完整构建，修复编译错误
- [x] 5.2 发布 exe
