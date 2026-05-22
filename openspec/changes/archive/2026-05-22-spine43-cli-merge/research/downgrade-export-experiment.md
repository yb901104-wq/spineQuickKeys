# 降级导出实验（搁置）

## 目标

Spine 4.3 CLI `--merge` 合并后的 `.spine` 文件是 4.3 格式。需要将其降级回原始低版本格式（如 4.2），同时保留全部动画数据。

## 探索的路径

### 方案A：CLI --update 降级

`Spine.com -u 4.2 -i merged.spine -o downgraded.spine`

`-u` 是指定编辑器版本加载，不是格式转换。4.2 编辑器无法打开 4.3 格式文件。

**结论：不可行。**

### 方案B：导出低版本 JSON → 重建 .spine

```
merged_4.3.spine
  ↓ -e json (version:4.2)
merged_4.2.json (运行时格式，含全部动画，结构已转 4.2 兼容)
  ↓ ???
merged_4.2.spine
```

关键问题：JSON 到 .spine 的转换无法用 CLI 完成。

| 途径 | 动画保留 | 原因 |
|------|:--------:|------|
| CLI `-r` 导入 JSON | ❌ | `-r` 从 JSON 导入不带动画（已知 CLI 硬限制） |
| 程序化写入 .spine | ⚠️ | 需要低版本 .spine 模板做 JSON 数据替换，可靠性未验证 |

### 方案C：JSON 导出 + 模板替换

1. CLI `-e json (version:4.2)` 导出低版本兼容 JSON（官方功能，安全）
2. 读取原始低版本 .spine 作为 JSON 模板
3. 把 JSON 数据替换进模板
4. 保存为新 .spine

格式转换由官方 CLI 处理（安全），但 JSON→.spine 容器化仍需程序操作，且 .spine 是否为纯 JSON 格式因版本而异。

**结论：半可行，未经官方验证，风险不可控。**

## 死亡原因

### 根本限制：CLI 缺少"导入数据"命令

Spine GUI 中"导入数据"（Import Data）可以直接从 JSON 导入骨架+动画到现有项目。但 CLI 没有对应的命令。CLI 的 `-r` 仅对应 GUI 的"导入项目"（Import Project）。

4.3 CLI 的三种 `-r` 模式：
```
Skeleton import:  -i path -o path -r              ← 导入骨架，无动画
Skeleton merge:   -i path -o path --merge -r       ← 合并骨架
Animation import: -i path -o path -a name -r       ← 导入动画（项目→项目）
```

没有"从 JSON 导入动画到项目"的通道。GitHub Issue #379 跟踪此功能，多年未实现。

### 已知矛盾

- `-r` 走的不是 CLI 里 GUI "导入数据"的通道
- GUI 里导入 JSON 可以直接带动画，但 CLI 没有等价命令
- `.spine` 项目文件的版本由写入它的编辑器版本决定，无法通过导出设置控制
- 运行时 JSON（`-e json`）的版本控制只影响运行时格式，不影响 `.spine` 项目格式

## 后续重启条件

1. Spine CLI 新增"从 JSON 导入数据并保留动画"的命令
2. 或 Issue #379 被解决
3. 或找到官方验证过的 `.spine` 格式无损往返方式
