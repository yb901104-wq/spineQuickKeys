## 1. SpineCliService 扩展

- [x] 1.1 新增 `GetProjectInfo(string path)` 方法：调用 `Spine -i` 解析输出，返回版本号和动画名列表
- [x] 1.2 新增 `MergeSkeleton(string source, string target, string? fromName, string? toName, string? version)` 方法：构建 `--merge` 命令
- [x] 1.3 新增 `ImportAnimations(string source, string target, List<string> animNames, string? version)` 方法：构建 `-a` 逐个导入命令

## 2. BatchCliWindow UI 改动（合并 Tab）

- [x] 2.1 源文件 ListView 新增"动画"列（第3列）
- [x] 2.2 源文件工具栏新增"动画选择"按钮，放在删除按钮后面
- [x] 2.3 实现双击源文件行弹出动画勾选对话框
- [x] 2.4 实现动画勾选对话框（CheckedListBox 列出 `Spine -i` 采集的动画名）
- [x] 2.5 添加 `--from`（源骨架名）和 `--to`（目标骨架名）输入框到合并 Tab
- [x] 2.6 在执行合并按钮后方新增 `☐ 实验功能：CLI骨架合并(4.3)` 复选框
- [x] 2.7 实现风险警告弹窗（确认/取消）

## 3. 实验合并执行逻辑

- [x] 3.1 实现目标文件复制为 `B_merged.spine`
- [x] 3.2 实现 `Spine -i` 采集版本号并做三分支兼容检测（<4.3.06 加 -u / 一致跳过 / 不一致报错终止）
- [x] 3.3 实现 `--from`/`--to` 输入框验证：用 `Spine -i` 检查骨架名是否存在
- [x] 3.4 实现骨架合并：调用 `MergeSkeleton` 执行 `--merge`
- [x] 3.5 实现动画导入：调用 `ImportAnimations` 逐个 `-a` 导入
- [x] 3.6 实现动画重名冲突检测与报错终止
- [x] 3.7 合并结果报告弹窗（成功/失败详情）
