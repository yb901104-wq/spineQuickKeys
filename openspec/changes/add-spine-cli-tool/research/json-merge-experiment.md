# JSON 数据强制合并骨架实验（搁置）

## 目标

通过 Spine CLI 将两个 `.spine` 骨架（源 A、目标 B）合并为单个骨架，保留双方的全部骨骼、插槽、皮肤、约束和**动画**。

## 方案尝试

### 流程设计

```
A.spine ──→ CLI -e json 导出 A.json（含 nonessential）
B.spine ──→ CLI -e json 导出 B.json
                ↓
      程序合并 JSON 数据
      ├─ 冲突检测：骨骼/插槽/皮肤/动画/约束命名
      ├─ 无冲突 → 将 A 数据追加到 B 结构中
      └─ 有冲突 → 报错中止
                ↓
      将合并 JSON 通过 CLI 导回 .spine
```

### 约束条件

- 仅支持 `.spine` 源文件
- 合并过程中检测命名冲突，有冲突即中止
- 先支持场景 A（源骨架无约束/网格变形，目标骨架有约束/网格变形）
- 骨架命名规则：`{目标文件名}_merged.spine`

## 死亡原因

### Spine CLI 硬限制：从 JSON 导入不包含动画

Spine 官方明确：

> **"The CLI's -r/--import command imports skeleton structure (bones, slots, skins, attachments) but not animations from JSON/binary data."**

即 `Spine.com -i merged.json -o merged.spine -r` 会丢弃所有动画数据。合并 JSON 再丰富，最后一步全丢。

### 替代路径：直接操作 .spine 文件

探索过程中发现 `.spine` 文件本质上是 JSON 格式，理论上可以直接读写：

```
B.spine ──→ 直接按 JSON 读取（保留全部数据含动画）
A.spine ──→ CLI -e json 导出 A.json
                ↓
      在 JSON 层级将 A 数据合并到 B 的 JSON 结构中
                ↓
      保存为 merged.spine（仍然是 .spine JSON 格式）
```

此路径完全绕过 `-r` 导入，动画零损失。但存在风险：

- `.spine` 格式与 `-e json` 导出格式可能结构不同
- JSON 合并需正确处理骨架层级、插槽顺序、动画 timeline 引用、deform、约束等跨引用关系
- 合并结果未经官方工具验证

### 最终判断

无论如何绕路，**动画数据的全自动合并无法在 Spine CLI 框架内可靠完成**：

| 路径 | 自动化 | 动画保留 | 可靠性 |
|------|:------:|:--------:|:------:|
| `-r` 集中管理（当前） | ✅ 全自动 | 各自独立骨架 | ✅ 官方 |
| JSON 合并 + CLI 导回 | ✅ 全自动 | ❌ 动画丢失 | ⚠️ |
| JSON 合并 + 直接写 .spine | ✅ 全自动 | ✅ | ❌ 未经官方验证 |
| 导出 JSON → GUI 导入动画 | ❌ 需手动 | ✅ | ✅ 官方 |

最终结论：**在 CLI 层面无法做到全自动的含动画骨架合并。** 与其造一个不可靠的半自动工具，不如保留当前经过验证的 `-r` 集中管理功能。

## 关键发现记录

### Spine CLI 能力矩阵

| 操作 | CLI 支持 | 动画保留 |
|------|:--------:|:--------:|
| `.spine` → `.json`（-e json） | ✅ | ✅ |
| `.spine` → 其他格式导出 | ✅ | ✅ |
| `.json` → `.spine`（-r import） | ✅ | ❌ 骨骼+插槽+皮肤 仅 |
| `.skel` → `.spine`（-r import） | ✅ | ❌ 同上 |
| `.spine` → `.spine`（-r 集中合并） | ✅ | ✅ 各自独立骨架 |
| GUI 导入动画到现有骨架 | ✅ | ✅ |

### JSON 导出要点

- `nonessential: true` 必须在 export settings JSON 中设置
- 骨骼名始终保留（不依赖 nonessential）
- 非必要数据包括：骨骼颜色、网格边缘、路径信息等

### 冲突检测范围

合并时需扫描的命名空间（均已确认在 JSON 结构中可读）：

- bones: name
- slots: name
- skins: name
- animations: name
- events: name
- constraints (ik/transform/path): name
- 各 skin 内同一 slot 下的 attachment name

## 后续重启条件

如果以下条件满足，可以重启此功能：

1. **Spine CLI 新增 `-r` 导入动画的支持** — 检查新版 Spine 更新日志
2. **或找到官方支持的 .spine → JSON → .spine 无损往返方式**
3. **或本项目自行实现可靠的 JSON 级合并器**（需充分测试动画完整性）

## 参考链接

- [Spine JSON Export Format](http://fr.esotericsoftware.com/spine-json-format)
- [Spine Command Line Interface](https://esotericsoftware.com/spine-command-line-interface)
- [Animation import from JSON file (forum)](https://eu.esotericsoftware.com/forum/d/29779-animation-import-from-json-file)
- [Losslessly exporting to JSON and back? (forum)](https://es.esotericsoftware.com/forum/d/17135-losslessly-exporting-to-json-and-back/3)
