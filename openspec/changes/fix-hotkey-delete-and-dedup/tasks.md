## 1. 加载去重

- [x] 1.1 `SpineHotkeyService.Load()` 中增加 `HashSet<string>` 去重，同名重复条目跳过

## 2. 空键跳过 TXT 写入

- [x] 2.1 `SpineHotkeyService.Save()` 中 Keys 为空/空白时跳过 TXT 写入行，但仍保留 annotations

## 3. 保存前提交编辑

- [x] 3.1 `SpineHotkeyEditor.BtnSave_Click()` 开头增加 `_dgv.EndEdit()` 调用

## 4. 验证

- [x] 4.1 构建项目确认无编译错误
- [x] 4.2 发布单文件 exe
