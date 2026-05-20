## 1. Copy source files into KeyMacro

- [x] 1.1 Create `KeyMacro/Forms/ReNameTool/` directory and copy Form1.cs, Form1.Designer.cs, Form1.resx from spine-tool-1

## 2. Adapt code to KeyMacro project

- [x] 2.1 Change namespace in Form1.cs and Form1.Designer.cs from `ReName` to `KeyMacro.Forms.ReNameTool`
- [x] 2.2 Update `typeof(Form1)` reference in Designer.cs `ComponentResourceManager` (no change needed — namespace update auto-aligns)
- [x] 2.3 Replace icon: remove `oubao.ico` dep, set `Icon = IconService.AppIcon` after `InitializeComponent()`

## 3. Add launch button in MainForm

- [x] 3.1 Add "图集工具" button to MainForm toolbar
- [x] 3.2 Wire click handler to `new ReNameTool.Form1().ShowDialog(this)`
