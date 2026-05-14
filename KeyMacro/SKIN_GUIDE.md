# 虚拟按键皮肤资源规范

## 目录结构

```
%APPDATA%\KeyMacro\skins\<皮肤名称>\
├── skin.json           # 皮肤配置（配色 + 元数据）
├── window_bg.png       # 窗口背景（9-slice 贴图，可选）
├── btn_normal.png      # 按钮正常状态
├── btn_hover.png       # 按钮悬停状态
├── btn_pressed.png     # 按钮按下状态
└── btn_active.png      # 按钮激活（循环发光）状态
```

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
    "btn_active_glow": "#00E5FF",
    "toolbar_bg": "#1A1A1A",
    "toolbar_text": "#AAAAAA"
  }
}
```

### 颜色字段说明

| 字段 | 用途 | 默认值 |
|------|------|--------|
| `window_bg` | 窗口背景色 | `#0D0D0D` |
| `window_border` | 窗口外边框 | `#000000` |
| `window_rim` | 窗口顶部亮线 | `#3C3C3C` |
| `btn_bg_top` | 按钮渐变顶部 | `#4A4A4A` |
| `btn_bg_bottom` | 按钮渐变底部 | `#383838` |
| `btn_text` | 按钮文字色 | `#E0E0E0` |
| `btn_dim_text` | 按钮副文字色 | `#888888` |
| `btn_active_glow` | 激活发光色 | `#00E5FF` |
| `toolbar_bg` | 工具栏背景 | `#1A1A1A` |
| `toolbar_text` | 工具栏文字 | `#AAAAAA` |

缺失的字段会自动使用默认值。

---

## 按钮图片规格

| 图片 | 建议尺寸 | 说明 |
|------|---------|------|
| `btn_normal.png` | 48×48 | 正常状态背景，撑满按钮区域 |
| `btn_hover.png` | 48×48 | 鼠标悬停（目前未使用，保留） |
| `btn_pressed.png` | 48×48 | 按下状态 |
| `btn_active.png` | 48×48 | 激活发光状态 |

图片会拉伸至按钮完整尺寸。如果某张图片不存在，对应状态回退到 GDI+ 颜色绘制。

---

## 窗口背景 9-slice 规则

`window_bg.png` 使用 9-slice 缩放，边距 4 像素：

```
┌────┬─────────────────────┬────┐
│ TL │         TOP         │ TR │    TL/TR/BL/BR = 4×4 角（不拉伸）
├────┼─────────────────────┼────┤
│    │                     │    │
│ L  │       CENTER        │ R  │    边（单向拉伸）
│    │                     │    │
├────┼─────────────────────┼────┤
│ BL │       BOTTOM        │ BR │
└────┴─────────────────────┴────┘

建议图片尺寸: 12×12 或更大
- 4px 角区域保持原始比例
- 边区域沿一个方向拉伸
- 中心区域双向拉伸
```

---

## 完整皮肤示例

```
%APPDATA%\KeyMacro\skins\dark-blue\
├── skin.json
├── window_bg.png       (12×12, 深蓝渐变)
├── btn_normal.png      (48×48, 蓝色按钮)
├── btn_pressed.png     (48×48, 暗蓝按下)
└── btn_active.png      (48×48, 亮蓝发光)
```
