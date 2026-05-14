using System.Text.Json;

namespace KeyMacro.Services;

public class SpineHotkeyEntry
{
    public string Name { get; set; } = "";
    public string Keys { get; set; } = "";
    public string? Section { get; set; }
    public string? ChineseNote { get; set; }
}

public class SpineHotkeyService
{
    public string FilePath { get; }
    private readonly string _annotationPath;

    private static readonly string TranslationPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KeyMacro", "spine_translations.txt");

    private static readonly string ProjectTranslationPath =
        Path.Combine(Directory.GetCurrentDirectory(), "spine_translations.txt");

    public SpineHotkeyService(string filePath)
    {
        FilePath = filePath;
        _annotationPath = filePath + ".annotations.json";
    }

    public List<SpineHotkeyEntry> Load()
    {
        var lines = File.ReadAllLines(FilePath);
        var entries = new List<SpineHotkeyEntry>();
        string? currentSection = null;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r', '\n');
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Section header: --- Section Name ---
            if (line.StartsWith("---") && line.EndsWith("---"))
            {
                currentSection = line.Trim('-').Trim();
                entries.Add(new SpineHotkeyEntry
                {
                    Name = line,
                    Keys = "",
                    Section = currentSection,
                    ChineseNote = null
                });
                continue;
            }

            // Hotkey line: Name: keys
            var colonIdx = line.IndexOf(':');
            if (colonIdx > 0)
            {
                var name = line[..colonIdx].TrimEnd();
                var keys = line[(colonIdx + 1)..].TrimStart();
                entries.Add(new SpineHotkeyEntry
                {
                    Name = name,
                    Keys = keys,
                    Section = currentSection
                });
            }
        }

        // Load translations from global dictionary
        EnsureTranslationFile();
        var translations = LoadTranslationFile();
        foreach (var entry in entries)
        {
            if (!entry.Name.StartsWith("---") && translations.TryGetValue(entry.Name, out var trans))
                entry.ChineseNote = trans;
        }

        // Load annotations from companion file (overrides translations)
        var annotations = LoadAnnotations();
        foreach (var entry in entries)
        {
            if (annotations.TryGetValue(entry.Name, out var note))
                entry.ChineseNote = note;
        }

        return entries;
    }

    public void Save(List<SpineHotkeyEntry> entries)
    {
        var annotations = new Dictionary<string, string>();

        using var writer = new StreamWriter(FilePath, false);
        foreach (var entry in entries)
        {
            if (entry.Name.StartsWith("---"))
            {
                writer.WriteLine(entry.Name);
                if (!string.IsNullOrEmpty(entry.ChineseNote))
                    annotations[entry.Name] = entry.ChineseNote;
            }
            else
            {
                writer.WriteLine($"{entry.Name}: {entry.Keys}");
                if (!string.IsNullOrEmpty(entry.ChineseNote))
                    annotations[entry.Name] = entry.ChineseNote;
            }
        }

        SaveAnnotations(annotations);
    }

    private Dictionary<string, string> LoadAnnotations()
    {
        try
        {
            if (!File.Exists(_annotationPath)) return [];
            var json = File.ReadAllText(_annotationPath);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Ensure the translation file exists, create from defaults if missing.
    /// Prefers project-adjacent file, falls back to APPDATA.
    /// </summary>
    public static void EnsureTranslationFile()
    {
        try
        {
            if (File.Exists(ProjectTranslationPath)) return;
            var dir = Path.GetDirectoryName(TranslationPath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (!File.Exists(TranslationPath))
                File.WriteAllText(TranslationPath, DefaultTranslations, System.Text.Encoding.UTF8);
        }
        catch { }
    }

    private static Dictionary<string, string> LoadTranslationFile()
    {
        var path = File.Exists(ProjectTranslationPath) ? ProjectTranslationPath : TranslationPath;
        try
        {
            if (!File.Exists(path)) return [];
            var lines = File.ReadAllLines(path, System.Text.Encoding.UTF8);
            var dict = new Dictionary<string, string>();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#')) continue;
                var eqIdx = trimmed.IndexOf('=');
                if (eqIdx > 0)
                {
                    var name = trimmed[..eqIdx].Trim();
                    var note = trimmed[(eqIdx + 1)..].Trim();
                    if (name.Length > 0 && note.Length > 0)
                        dict[name] = note;
                }
            }
            return dict;
        }
        catch
        {
            return [];
        }
    }

    public static string GetTranslationPath() => TranslationPath;

    /// <summary>
    /// Convert WinForms key name (e.g. "OemPeriod") to Spine format (e.g. "PERIOD").
    /// Used when recording keys via HotkeyRecorderForm.
    /// </summary>
    public static string ToSpineFormat(string winFormsHotkey)
    {
        if (string.IsNullOrWhiteSpace(winFormsHotkey)) return "";

        var parts = winFormsHotkey.Split('+');
        var result = new List<string>();
        foreach (var part in parts)
        {
            result.Add(ReverseMap.TryGetValue(part, out var spine) ? spine : part);
        }
        return string.Join(" + ", result);
    }

    private static readonly Dictionary<string, string> ReverseMap = new()
    {
        // WinForms Keys enum → Spine format (uppercase)
        ["OemPeriod"] = "PERIOD",
        ["Oemcomma"] = "COMMA",
        ["OemMinus"] = "MINUS",
        ["Subtract"] = "NUMPAD_MINUS",
        ["Add"] = "NUMPAD_PLUS",
        ["Oemplus"] = "PLUS",
        ["Oem2"] = "SLASH",
        ["OemOpenBrackets"] = "LEFT_BRACKET",
        ["OemCloseBrackets"] = "RIGHT_BRACKET",
        ["Oem5"] = "BACKSLASH",
        ["Oem1"] = "SEMICOLON",
        ["Oem7"] = "APOSTROPHE",
        ["Space"] = "SPACE",
        ["Escape"] = "ESCAPE",
        ["PageDown"] = "PAGE_DOWN",
        ["PageUp"] = "PAGE_UP",
        ["Home"] = "HOME",
        ["End"] = "END",
        ["Tab"] = "TAB",
        ["Enter"] = "ENTER",
        ["Delete"] = "DELETE",
        ["Back"] = "BACKSPACE",
        ["Insert"] = "INSERT",
        ["Up"] = "UP",
        ["Down"] = "DOWN",
        ["Left"] = "LEFT",
        ["Right"] = "RIGHT",
        ["NumLock"] = "NUMLOCK",
        ["Scroll"] = "SCROLLLOCK",
        ["PrintScreen"] = "PRINTSCREEN",
        ["Pause"] = "PAUSE",
        ["CapsLock"] = "CAPSLOCK",
        ["Capital"] = "CAPSLOCK",
        ["Next"] = "PAGE_DOWN",
        ["Prior"] = "PAGE_UP",
        ["D0"] = "0",
        ["D1"] = "1",
        ["D2"] = "2",
        ["D3"] = "3",
        ["D4"] = "4",
        ["D5"] = "5",
        ["D6"] = "6",
        ["D7"] = "7",
        ["D8"] = "8",
        ["D9"] = "9",
        ["NumPad0"] = "NUMPAD_0",
        ["NumPad1"] = "NUMPAD_1",
        ["NumPad2"] = "NUMPAD_2",
        ["NumPad3"] = "NUMPAD_3",
        ["NumPad4"] = "NUMPAD_4",
        ["NumPad5"] = "NUMPAD_5",
        ["NumPad6"] = "NUMPAD_6",
        ["NumPad7"] = "NUMPAD_7",
        ["NumPad8"] = "NUMPAD_8",
        ["NumPad9"] = "NUMPAD_9",
    };

    private static readonly string DefaultTranslations = """
Undo=撤销
Redo=重做
Setup/Animate Mode=切换Setup/Animate模式
Rulers=标尺
Zoom To Fit=放大到适配
Zoom To 100%=100%缩放
Lock Zoom=锁定缩放
Fullscreen=全屏
Always On Top=总在最前
Instant Tooltips=显示工具提示
Hotkey Popups=快捷键弹出
Pan Drag=平移拖拽
Zoom Drag=缩放拖拽
Pan Move=平移移动
Zoom Move=缩放移动
Top=跳转到顶部
Bottom=跳转到底部
Focus - Dopesheet=聚焦摄影表
Focus - Graph=聚焦图表
Focus - Tree=聚焦层级树
Focus - Viewport=聚焦视图
Focus - Weights=聚焦权重
Next Skin=下一个皮肤
Previous Skin=上一个皮肤
Mute=静音
Highlight Pixels=高亮像素
Disable Constraints=禁用约束
Physics - Simulate=物理模拟
Physics - Deterministic=物理确定性
Minimize Views=最小化视图
Animations View=动画视图
Audio View=音频视图
Dopesheet View=摄影表视图
Ghosting View=幻影视图
Graph View=图表视图
Curves View=曲线视图
Metrics View=指标视图
Mesh Tools View=网格工具视图
Outline View=轮廓视图
Playback View=播放视图
Preview View=预览视图
Skins View=皮肤视图
Slot Color View=插槽颜色视图
Timeline View=时间轴视图
Tree View=层级树视图
Weights View=权重视图
Rotate Tool=旋转工具
Translate Tool=移动工具
Scale Tool=缩放工具
Shear Tool=切变工具
Pose Tool=姿态工具
Create Tool=创建工具
Weights Tool=权重工具
Last Tool=上一个工具
Local Axes=局部轴
Constrained Axes=约束轴
Parent Axes=父轴
World Axes=世界轴
Bone Compensation=骨骼补偿
Attachment Compensation=附件补偿
Pixel Snapping=像素吸附
Nudge Up=向上微调
Nudge Up (10)=向上微调10
Nudge Down=向下微调
Nudge Down (10)=向下微调10
Nudge Left=向左微调
Nudge Left (10)=向左微调10
Nudge Right=向右微调
Nudge Right (10)=向右微调10
Mesh Tools - Soft Selection=网格工具-软选择
Mesh Tools - Hull Vertices=网格工具-外壳顶点
Brush+=笔刷+
Brush-=笔刷-
Feather+=羽化+
Feather-=羽化-
Weights - Direct Mode=权重-直接模式
Weights - Add Mode=权重-添加模式
Weights - Remove Mode=权重-移除模式
Weights - Replace Mode=权重-替换模式
Weights - Strength (+10)=权重强度+10
Weights - Strength (-10)=权重强度-10
Weights - Strength (+1)=权重强度+1
Weights - Strength (-1)=权重强度-1
Weights - Bind=权重绑定
Weights - Remove=权重移除
Weights - Update Bindings=更新权重绑定
Weights - Smooth=权重平滑
Weights - Auto=自动权重
Weights - Prune=权重剪枝
Weights - Pies=权重饼图
Weights - Overlay=权重叠加
Weights - Selected=已选权重
Weights - Select Bones=选择权重骨骼
Weights - Swap=交换权重
Weights - Weld All=焊接所有权重
Weights - Weld Overlapping=焊接重叠权重
Select Bones=选择骨骼
Show Bones=显示骨骼
Bone Names=显示骨骼名称
Select Images=选择图片
Show Images=显示图片
Image Names=显示图片名称
Select Others=选择其他
Show Others=显示其他
Other Names=显示其他名称
Auto Scroll=自动滚动
Scroll To Selected=滚动到选中
Deselect=取消选择
Select All=全选
Select - Bones=选择骨骼
Select - Child Bones=选择子骨骼
Select - Descendant Bones=选择后代骨骼
Select - Colored Bones=选择彩色骨骼
Select - Slots=选择插槽
Select - Draw Order=选择绘制顺序
Select - Attachments=选择附件
Select - Visible Attachments=选择可见附件
Select - IK Constraints=选择IK约束
Select - Path Constraints=选择路径约束
Select - Physics Constraints=选择物理约束
Select - Sliders=选择滑块
Select - Transform Constraints=选择变换约束
Previous Selection=上一个选择
Next Selection=下一个选择
Hide Selection=隐藏选择
Cut=剪切
Copy=复制
Paste=粘贴
Delete=删除
Rename=重命名
Duplicate=复制
Visibility=可见性
Show All Bones/Slots/Constraints=显示所有骨骼/插槽/约束
Draw Order Down=绘制顺序下移
Draw Order Down (5)=绘制顺序下移5
Draw Order Up=绘制顺序上移
Draw Order Up (5)=绘制顺序上移5
Replace=替换
Parent=设为父级
Child=设为子级
Previous Sibling=上一个同级
Next Sibling=下一个同级
Parent Bone=父骨骼
Child Bone=子骨骼
Previous Sibling Bone=上一个同级骨骼
Next Sibling Bone=下一个同级骨骼
Tree Up=树向上
Tree Down=树向下
Collapse=折叠
Expand=展开
Expand/Collapse=展开/折叠
Scroll To Selection=滚动到选中
Search=搜索
Search - Next=搜索下一个
Search - Previous=搜索上一个
Search - Select All=搜索全选
Search - Clear=清除搜索
Show Slot Folders=显示插槽文件夹
Show Slot Paths=显示插槽路径
Show All Skin Attachments=显示所有皮肤附件
Hide Skeleton Names=隐藏骨骼名称
Hide Skin Names=隐藏皮肤名称
Hide Skin Bones/Constraints=隐藏皮肤骨骼/约束
Hide Viewport Skin Bones=隐藏视口皮肤骨骼
Main Menu=主菜单
New Project=新建项目
Open Project=打开项目
Open Project (Browse)=浏览打开项目
Save Project=保存项目
Save Project As=另存项目
New Skeleton=新建骨架
Import Project=导入项目
Import Data=导入数据
Import PSD=导入PSD
Export=导出
Export JSON=导出JSON
Export Binary=导出二进制
Export GIF=导出GIF
Export PNG=导出PNG
Export APNG=导出APNG
Export WEBP=导出WEBP
Export AWEBP=导出AWEBP
Export PSD=导出PSD
Export JPEG=导出JPEG
Export AVI=导出AVI
Export MOV=导出MOV
Export WEBM=导出WEBM
Repeat Last Export=重复上次导出
Texture Packer=纹理打包器
Repeat Last Texture Packer=重复上次纹理打包
Texture Unpacker=纹理解包器
Settings=设置
Set Parent=设置父节点
New Bone=新建骨骼
New Slot=新建插槽
New Skin Placeholder=新建皮肤占位
New Bounding Box=新建边界框
New Clipping=新建裁剪
New Path=新建路径
New Point=新建点
New Animation=新建动画
New Event=新建事件
New Skin=新建皮肤
New IK Constraint=新建IK约束
New Path Constraint=新建路径约束
New Physics Constraint=新建物理约束
New Slider=新建滑块
New Transform Constraint=新建变换约束
New Folder=新建文件夹
Color=颜色
Mesh - Freeze=网格冻结
Mesh - Reset=网格重置
Mesh - Wireframe=网格线框
Mesh - Edit Mesh=编辑网格
Edit Mesh - Modify=编辑网格-修改
Edit Mesh - Create=编辑网格-创建
Edit Mesh - Delete=编辑网格-删除
Edit Mesh - New=编辑网格-新建
Edit Mesh - Reset=编辑网格-重置
Edit Mesh - Generate=编辑网格-生成
Edit Mesh - Trace=编辑网格-追踪
Edit Mesh - Refine=编辑网格-细化
Edit Mesh - Triangles=编辑网格-三角化
Edit Mesh - Dim=编辑网格-暗淡
Edit Mesh - Isolate=编辑网格-隔离
Edit Mesh - Deformed=编辑网格-变形
Auto Key=自动关键帧
Animation Clean Up=动画清理
Next Animation=下一个动画
Previous Animation=上一个动画
Track Current Frame=跟踪当前帧
Frame Keys=帧关键帧
Setup Draw Order=设置绘制顺序
Setup Pose=设置姿态
Sync Dopesheet=同步摄影表
Lock Dopesheet=锁定摄影表
Refresh Dopesheet=刷新摄影表
Select Dopesheet Bones=选择摄影表骨骼
Dopesheet Filter=摄影表过滤器
Dopesheet Rows=摄影表行
Dopesheet Toolbar=摄影表工具栏
Set Loop Start=设置循环开始
Set Loop End=设置循环结束
Lock Graph=锁定图表
Refresh Graph=刷新图表
Select Graph Bones=选择图表骨骼
Graph Filter=图表过滤器
Graph Toolbar=图表工具栏
Graph Rows=图表行
Hide Bezier Handles=隐藏贝塞尔手柄
Graph Frame=图表帧
Graph Auto Frame=图表自动帧
Bezier Handle - Auto=自动贝塞尔手柄
Bezier Handle - Separate=分离贝塞尔手柄
Bezier Handle - Bounce=弹跳贝塞尔手柄
Bezier Handle - Flat=平坦贝塞尔手柄
Bezier Handle - Ease Out=缓出贝塞尔手柄
Bezier Handle - Ease In=缓入贝塞尔手柄
Bezier Handle - Ease=缓动贝塞尔手柄
Graph Snapping=图表吸附
Graph X=图表X轴
Graph X - Momentary=图表X轴临时
Graph Y=图表Y轴
Graph Y - Momentary=图表Y轴临时
Graph Retiming - None=图表重定时-无
Graph Retiming - Shape=图表重定时-形状
Graph Retiming - Value=图表重定时-值
Graph Revaluing - Scale=图表重估值-缩放
Graph Store=图表存储
Graph Store Swap=图表存储交换
Favor Tool=偏好工具
Favor+ (5)=偏好+5
Favor+ (10)=偏好+10
Favor+ (15)=偏好+15
Favor- (5)=偏好-5
Favor- (10)=偏好-10
Favor- (15)=偏好-15
Favor - Favor=偏好-偏好
Favor - Blend=偏好-混合
Favor - Shift=偏好-位移
Favor - Linear=偏好-线性
Favor - Average (curve)=偏好-平均曲线
Favor - Average (frame)=偏好-平均帧
Favor - Average (all)=偏好-平均全部
Favor - Default=偏好-默认
Favor - Setup=偏好-设置
Favor - Store=偏好-存储
Key Edited=为已编辑设关键帧
Key Active=为激活设关键帧
Key Selected=为选中设关键帧
Key Shown=为显示设关键帧
Key Rotation=旋转关键帧
Key Translate=位移关键帧
Key Translate X=位移X关键帧
Key Translate Y=位移Y关键帧
Key Scale=缩放关键帧
Key Scale X=缩放X关键帧
Key Scale Y=缩放Y关键帧
Key Shear=切变关键帧
Key Shear X=切变X关键帧
Key Shear Y=切变Y关键帧
Key Color=颜色关键帧
Key Attachment=附件关键帧
Select Keys=选择关键帧
Stepped Curve=步进曲线
Linear Curve=线性曲线
Bezier Curve=贝塞尔曲线
Shift Keys=位移关键帧
Offset Keys=偏移关键帧
Adjust Keys=调整关键帧
Key Constrained=约束关键帧
Play Forward=向前播放
Play Backward=向后播放
Play Forward / Reset=向前播放/重置
Play Backward / Reset=向后播放/重置
Stop=停止
Repeat=重复
Stepped=步进
Interpolated=插值
First Key=首帧
Last Key=尾帧
Next Key=下一关键帧
Previous Key=上一关键帧
Next Frame=下一帧
Next Frame (10)=下10帧
Previous Frame=上一帧
Previous Frame (10)=上10帧
Loop Start=循环开始
Loop End=循环结束
Speed - Slower=减速
Speed - Faster=加速
Speed - 100%=正常速度
Timeline Pan Drag=时间轴平移拖拽
Timeline Pan Move=时间轴平移移动
Timeline Frame Drag=时间轴帧拖拽
Timeline Frame Move=时间轴帧移动
Ghosting=幻影
Ghosting - Frames Before=幻影-前帧数
Ghosting - Frames After=幻影-后帧数
Ghosting - Frames Current=幻影-当前帧
Ghosting - Keys Before=幻影-前关键帧
Ghosting - Keys After=幻影-后关键帧
Ghosting - Motion Vectors Before=幻影-前运动向量
Ghosting - Motion Vectors After=幻影-后运动向量
Ghosting - Anchor=幻影-锚点
Ghosting - On Top=幻影-置顶
Ghosting - Loop=幻影-循环
Ghosting - Lock=幻影-锁定
Ghosting - Refresh=幻影-刷新
Ghosting - Selection Only=幻影-仅选中
Ghosting - Selection Only=幻影-仅选中
""";

    private void SaveAnnotations(Dictionary<string, string> annotations)
    {
        try
        {
            var json = JsonSerializer.Serialize(annotations, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_annotationPath, json);
        }
        catch { }
    }
}
