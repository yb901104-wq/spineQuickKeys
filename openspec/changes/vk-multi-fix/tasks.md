## 1. 多窗口皮肤加载

- [ ] 1.1 ApplyDefaultSkin 中放宽 skinPath 判断，覆盖空字符串
- [ ] 1.2 确认旧窗口创建后再次 LoadAll 时自动补上默认皮肤

## 2. 按钮名跨窗口唯一

- [ ] 2.1 VirtualKeyWindow 新增 CheckButtonNameUnique 方法，扫描所有 VK 窗口
- [ ] 2.2 AddButton 时自动跳过已存在的编号名
- [ ] 2.3 改名时校验唯一性，重复则提示拒绝

## 3. 解绑 + 热键释放

- [ ] 3.1 SequenceEditor 保存时若 TriggerVkButtonName 被清空，调用 MainForm.SyncVkButtonBindings 或直清除对应 BindActionId
- [ ] 3.2 SequenceEditor 触发快捷键增加"清除"按钮，置空 _txtHotkey
- [ ] 3.3 清除热键后同步更新 HotkeyService

## 4. 复制步骤 / 复制序列

- [ ] 4.1 SequenceEditor 步骤 DataGridView 增加"复制"按钮列
- [ ] 4.2 主窗口工具栏增加"复制序列"按钮
- [ ] 4.3 复制序列插入同名 "_副本" 序列到列表

## 5. 默认值修正

- [ ] 5.1 MacroStep 模型 PressMode 默认改为 PressMode.Point
- [ ] 5.2 MacroSequence LoopIntervalMs 默认 200 → 100

## 6. 显示/隐藏按钮状态同步

- [ ] 6.1 VkWindowManager.RefreshList 遍历 _vkWindows 获取实际可见状态
- [ ] 6.2 设按钮文字为"隐藏"（窗口可见时）或"显示"（窗口隐藏时）

## 7. 验证

- [ ] 7.1 编译通过
- [ ] 7.2 功能和默认值确认
