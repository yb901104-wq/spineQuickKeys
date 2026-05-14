## 1. 无标题栏窗口基础改造

- [x] 1.1 VirtualKeyWindow FormBorderStyle 改为 None，移除 Size/MinimumSize 固定值
- [x] 1.2 实现右下角缩放手柄（Panel + Paint 自绘斜线 + MouseDown/Move 缩放）
- [x] 1.3 缩放手柄拖拽缩放与 UpdateScale() 联动
- [x] 1.4 保留现有的自定义边框绘制，适配 FormBorderStyle=None

## 2. 工具栏

- [x] 2.1 在 FlowLayoutPanel 上方添加工具栏 Panel（Dock=Top, Height=28）
- [x] 2.2 工具栏内容：目标窗口名、按钮数量、关闭按钮
- [x] 2.3 工具栏拖拽移动窗口
- [x] 2.4 窗口锁定/解锁控制工具栏显示隐藏和拖拽

## 3. 布局模式切换

- [x] 3.1 新增 _singleLineMode 字段，实现单排/多排切换逻辑
- [x] 3.2 单排模式：WrapContents=false + AutoSize
- [x] 3.3 多排模式：WrapContents=true + 固定宽度
- [x] 3.4 布局模式持久化到 LayoutData

## 4. 右键菜单更新

- [x] 4.1 BuildBlankMenu 增加"单排/多排"切换项
- [x] 4.2 BuildBlankMenu 增加"缩放 >"子菜单（50/75/100/150/200%）
- [x] 4.3 BuildBlankMenu 增加"关闭窗口"项

## 5. 皮肤系统

- [x] 5.1 新建 VkSkinLoader.cs 服务类（Load/GetColor/GetButtonImage/GetWindowBackground）
- [x] 5.2 实现 SkinData 数据类（colors 字典 + 图片缓存）
- [x] 5.3 实现 skin.json 解析 + 颜色获取（缺失回退默认值）
- [x] 5.4 实现 PNG 图片加载 + 缓存
- [x] 5.5 实现 9-slice 贴图绘制
- [x] 5.6 VirtualLayoutSerializer.LayoutData 增加 SkinPath + SingleLineMode 字段
- [x] 5.7 VirtualKeyWindow 构造时加载皮肤

## 6. 皮肤渲染集成

- [x] 6.1 VirtualKeyWindow Paint 事件中使用皮肤颜色/贴图绘制边框
- [x] 6.2 窗口背景/边框使用皮肤颜色和贴图
- [x] 6.3 工具栏颜色使用皮肤配置

## 7. 资源规范文档

- [x] 7.1 创建 SKIN_GUIDE.md，包含目录结构、文件名约定、9-slice 规则、skin.json 字段说明
