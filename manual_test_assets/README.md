# 手工验证测试素材投放目录

请把手工验证需要的测试文件放在本目录下。建议只放测试文件，不放真实生产项目。

## 目录约定

| 目录 | 需要放入的文件 |
| --- | --- |
| `spine_hotkeys/` | Spine hotkey TXT 文件，用于热键编辑、导入导出、重复项诊断。 |
| `kmp/` | `.kmp` 数据包，用于统一导入导出测试。 |
| `batch_copy/source/` | 批量复制源文件，建议包含 PNG、TXT、同名文件样本。 |
| `batch_copy/targets/` | 批量复制目标目录，可提前放入同名文件制造冲突。 |
| `cli/projects/` | Spine CLI 测试项目，如 `.spine`、`.json`、`.skel`。 |
| `cli/export_configs/` | CLI 导出配置文件，如 `export.json`、自定义导出配置。 |

## 当前需要你手动提供的素材

1. 至少一份由 Spine 初始化生成的全新 hotkey TXT。
2. 如果要验证历史重复问题，请提供曾经导入后出现重复项的 hotkey TXT 或 `.kmp`。
3. 如果要验证 CLI 功能，请提供至少两个小型 Spine 项目和对应 export 配置。
4. 如果要验证批量复制冲突，请在 `batch_copy/source/` 放 2-3 个文件，并在某个 `batch_copy/targets/` 子目录中放同名文件。

## 截图目录

验证截图统一放到：

```text
docs/verification/screenshots/
```

截图文件名建议使用：

```text
CASE-ID_step_result.png
```
