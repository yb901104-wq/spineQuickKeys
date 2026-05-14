# 虚拟按键皮肤资源规范

> **说明**：本文档是开发阶段的 UI 资源规范，非运行时皮肤定制系统。
> 资源文件放在项目源码目录中，随源码管理。PNG 存在时优先使用图片，不存在时回退 GDI+ 绘制。

## 目录结构

```
<项目目录>\skins\<皮肤名称>\
├── skin.json           # 皮肤配置（配色 + 元数据）
├── window_bg.png       # 窗口背景（9-slice 贴图，可选）
├── btn_normal.png      # 按钮正常状态
├── btn_hover.png       # 按钮悬停状态
├── btn_pressed.png     # 按钮按下状态
└── btn_active.png      # 按钮激活（循环发光）状态
```

例如：`KeyMacro\skins\my-theme\`。

皮肤名称在 `virtual_layout.json` 的 `SkinPath` 字段中指定：
```json
{
  "SkinPath": "my-skin"
}
```

设为 `null` 或空字符串使用默认 GDI+ 绘制。

---

## skin.json 格式

```json
{
  "name": "my-skin",
  "author": "作者名",
  "version": "1.0",
  "colors": {
    "window_bg": "#0D0D0D",
    "window_border": "#000000",
    "window_rim": "#3C3C3C",
    "btn_bg_top": "#4A4A4A",
    "btn_bg_bottom": "#383838",
    "btn_text": "#E0E0E0",
    "btn_dim_text": "#888888",
    "btn_active_glow": "#00E5FF"
  }
}
```

### 颜色字段说明

| 字段 | 用途 | 默认值 |
|------|------|--------|
| `window_bg` | 窗口背景色 | `#0D0D0D` |
| `window_border` | 窗口外边框/按钮外边框 | `#000000` |
| `window_rim` | 按钮顶部亮线 | `#3C3C3C` |
| `btn_bg_top` | 按钮渐变顶部 | `#4A4A4A` |
| `btn_bg_bottom` | 按钮渐变底部 | `#383838` |
| `btn_text` | 按钮文字色 | `#E0E0E0` |
| `btn_dim_text` | 按钮副文字色 | `#888888` |
| `btn_active_glow` | 激活发光色 | `#00E5FF` |

缺失的字段会自动使用默认值。

---

## 按钮类型与尺寸

虚拟按键有三种样式，基础尺寸如下（缩放比例可调）：

| 样式 | 基础宽度 | 基础高度 | 用途 |
|------|---------|---------|------|
| SmallIcon | 48 | 48 | 标准按钮，图标 + 单行名称 |
| LargeIcon | 96 | 48 | 大按钮，左侧大图标 + 右侧名称/状态 |
| LoopIcon | 110 | 48 | 循环按钮，左侧图标/名称 + 右侧次数输入框 |

## 按钮图片规格

图片按按钮样式区分文件名，加载时优先加载样式专用图，不存在时回退到通用图片：

| 样式 | 正常态 | 按下态 | 激活态 |
|------|--------|--------|--------|
| SmallIcon （48×48） | `btn_small_normal.png` | `btn_small_pressed.png` | `btn_small_active.png` |
| LargeIcon （96×48） | `btn_large_normal.png` | `btn_large_pressed.png` | `btn_large_active.png` |
| LoopIcon （110×48） | `btn_loop_normal.png` | `btn_loop_pressed.png` | `btn_loop_active.png` |
| **通用回退** | `btn_normal.png` | `btn_pressed.png` | `btn_active.png` |

加载逻辑：先找 `btn_small_normal.png`，不存在则用 `btn_normal.png`。因此初期可以只准备一套通用图，后续按样式逐个细化。

图片会**拉伸至按钮完整尺寸**，建议按最大尺寸（110×48）设计。
如果某张图片不存在，对应状态回退到 GDI+ 颜色绘制。

---

## 窗口背景 9-slice 规则

`window_bg.png` 使用 9-slice 缩放，边距 10 像素（与窗口 Panel Padding 一致）：

```
┌────┬─────────────────────────┬────┐
│ TL │          TOP            │ TR │    TL/TR/BL/BR = 10×10 角（不拉伸）
├────┼─────────────────────────┼────┤
│    │                         │    │
│ L  │        CENTER           │ R  │    边（单向拉伸）
│    │                         │    │
├────┼─────────────────────────┼────┤
│ BL │        BOTTOM           │ BR │
└────┴─────────────────────────┴────┘

建议图片尺寸: 30×30 或更大
- 10px 角区域保持原始比例
- 边区域沿一个方向拉伸
- 中心区域双向拉伸
```

---

## 完整皮肤示例

```
KeyMacro\skins\dark-blue\
├── skin.json
├── window_bg.png       (12×12, 深蓝渐变)
├── btn_normal.png      (48×48, 蓝色按钮)
├── btn_pressed.png     (48×48, 暗蓝按下)
└── btn_active.png      (48×48, 亮蓝发光)
```
