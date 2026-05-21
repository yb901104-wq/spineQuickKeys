## Context

KeyMacro 现有 Spine 工具（ReNameTool、BatchCopy）均使用 WinForms。新 CLI 工具沿用同一技术栈，通过 TabControl 分页组织两个功能模块。

## Goals / Non-Goals

**Goals:**
- SpineCliService 封装所有 CLI 调用逻辑
- BatchCliWindow 提供合并/导出两个 Tab
- 文件遍历、勾选、状态标注等交互体验

**Non-Goals:**
- 不修改 Spine CLI 本身
- 不替代现有 ReNameTool/BatchCopy

## Decisions

**SpineCliService 设计：**
```csharp
public class SpineCliService
{
    public string SpinePath { get; set; }  // Spine.com 路径
    public bool DetectFromRegistry();      // 自动查找
    public Task<CliResult> Export(...);
    public Task<CliResult> Pack(...);
    public Task<CliResult> ImportMerge(...);
    public Task<CliResult> UpdateVersion(...);
    public Task<CliResult> RunAsync(string args);
}
```

**RunAsync 内部：**
```
Process.Start(SpinePath, args)
├── RedirectStandardOutput / Error
├── await process.WaitForExitAsync()
└── 返回 CliResult { ExitCode, Output, Error }
```

**BatchCliWindow 布局：**
```
顶部: Spine.com 路径输入框 + 检测按钮 + 选择按钮

TabPage "合并":
├── 左侧源文件 ListView (FilePath列)
├── 右侧目标文件 ListView (FilePath列)
├── 各自 + / - 按钮
├── 目标添加 → 遍历子目录 .spine → SubfolderSelectDialog 勾选
├── 各自载入时检测 export.json → 状态列
├── 校验: 只能一边多选 → 否则弹窗阻止
└── [执行合并] 按钮

TabPage "批量导出":
├── 源目录选择 → 遍历 .spine → SubfolderSelectDialog 勾选
├── 文件 ListView (文件名 | export.json 状态 | 路径)
├── [刷新] 按钮
├── 输出目录输入框 + [浏览]
├── [导出] → Spine -e <config>
├── [单纹理图] → Spine -p <name>
└── [改版本号] → InputBox → --update
```

**合并输出规则：**
- 在目标文件同目录生成 `{原名}_merged.spine`
- 不修改原始文件
- temp 桥梁文件放在 `%TEMP%\KeyMacro\` 下，合并后删除

**日志规则：**
- 输出目录中 `cli_export_log.txt`
- 每次追加，时间戳开头
- `File.AppendAllText`

## Risks / Trade-offs

- **[中]** Spine.com 路径不确定 — 通过注册表检测 + 手动选择 + 记忆到配置
- **[低]** CLI 执行错误 — 捕获 stderr 展示给用户
