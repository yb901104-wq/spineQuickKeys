# KeyMacro 图标资源指南

## 目录结构

```
icons/
├── ICON_GUIDE.md       # 本文件
├── icons.json           # 图标配置文件（可选）
└── app.ico              # 应用程序图标（可选，多尺寸 .ico）
```

## app.ico

应用程序图标，用于：
- 桌面 / 资源管理器中的 .exe 文件图标
- 主窗口标题栏和任务栏
- 系统托盘图标
- 所有子窗口（序列编辑器、Spine 编辑器、虚拟按键窗口等）

### 格式要求

- 文件格式: Windows ICO (.ico)
- 建议包含尺寸: 16×16, 32×32, 48×48, 256×256（PNG 压缩）
- 必须放在 `icons/app.ico` 位置

### 回退行为

无 `app.ico` 时，系统自动使用代码生成的默认图标（蓝色 `#0078D7` 底 + 白色 "K" 字）。

## icons.json

可选配置文件，格式：

```json
{
  "name": "default",
  "author": "",
  "version": "1.0"
}
```

## 编译说明

所有 `icons/` 目录下的文件通过 `.csproj` 的 `<EmbeddedResource Include="icons\**" />` 自动编译入程序集。
`app.ico` 还通过 `<ApplicationIcon>` 写入 .exe 文件的 PE 头部。
