## 1. Data Model — AnnotationEntry

- [x] 1.1 在 `SpineHotkeyService.cs` 中新增 `AnnotationEntry` record（name, note 字段）
- [x] 1.2 `LoadAnnotations()` 改为返回 `List<AnnotationEntry>`，先尝试数组反序列化，失败回退字典格式

## 2. SpineHotkeyService 保存逻辑

- [x] 2.1 `SaveAnnotations()` 改为接收 `List<AnnotationEntry>`，写出 JSON 数组格式
- [x] 2.2 `Save()` 中构建 annotations 列表的逻辑改为创建 `List<AnnotationEntry>` 而非 `Dictionary<string, string>`
- [x] 2.3 `Load()` 中 annotations 合并逻辑适配新返回类型（遍历列表按 name 匹配）

## 3. SpineHotkeyEditor 保存修复

- [x] 3.1 `BtnSave_Click()` 中保存循环改为按 Name 遍历 grid 行，在 `_entries` 中 FirstOrDefault 匹配后回写

## 4. 释放按钮修复

- [x] 4.1 `OpenSpineEditor()` 中在 `editor.ShowDialog()` 和 `editorFromDlg.ShowDialog()` 后各添加 `UpdateSpineReleaseButton()` 调用

## 5. 验证

- [x] 5.1 构建项目，确认无编译错误
- [x] 5.2 发布单文件 exe 测试
