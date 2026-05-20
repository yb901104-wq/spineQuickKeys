#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using KeyMacro.Services;

namespace KeyMacro.Forms.ReNameTool
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Icon = IconService.AppIcon;
        }

        private void button1_Click(object sender, EventArgs e)//选择文件到列表1
        {
            DialogResult dialogResult1 = openFileDialog1.ShowDialog();//创建一个枚举类对话框变量，然后把变量结果定义为dialogResult1

            if (dialogResult1 == DialogResult.OK)  //判断这个枚举变量是否选择OK
            {
                string[] filepaths = openFileDialog1.FileNames.ToArray();//定义字符 变量“filepaths”，这个变量等于利用openFileDialog1.FileNames抓取所选所有文件的完整路径并返回字符串数组

                foreach (string filepath in filepaths) //遍历所选择的所有filepaths里的数组，发牌员从"牌堆"filepaths中依次发出了“牌”filepath.接下来的{}中表达的要对发出的牌做的事
                {
                    listBox1.Items.Add(filepath);//要对牌做的事：把每一张遍历到的filepath依次添加到listbox1列表

                }

            }

        }

        private void button2_Click(object sender, EventArgs e)//清空列表1
        {
            listBox1.Items.Clear();//清理listbox1列表里的内容
        }

        private void button3_Click(object sender, EventArgs e)//替换关键字
        {
            //foreach (string file in listBox1.Items) //(旧方案)遍历listbox1列表里的所有数组，并获取里面的每一个文件的路径file
            for (int i = 0; i < listBox1.Items.Count; i++)
            {
                //string filename = Path.GetFileName(file);//(旧方案)创建变量filename等于获取（getfilename）每一个文件file的文件名和扩展名
                //string filepath = Path.GetDirectoryName(file);//(旧方案)创建变量filepath等于获取（GetDirectoryName）每一个文件file的路径

                string oldfullpath = listBox1.Items[i].ToString();

                string filename = Path.GetFileName(oldfullpath);
                string filepath = Path.GetDirectoryName(oldfullpath);


                if (filename.Contains(textBox1.Text)) //判断！！获取的每一个文件名中，是否包含输入框textbox1中所输入的文字。
                {
                    string newfilename = filename.Replace(textBox1.Text, textBox2.Text);//如果存在就创建变量newfilenames，该变量等于
                    string newfullpath = Path.Combine(filepath, newfilename);
                    
                    if (!File.Exists(newfullpath)) //判断！！路径+修改后的新名字（包含扩展名）的名字是否已经存在。!感叹号为逻辑反向，在此处，变为新名字存在即为否定，不存在即为肯定
                    {
                        //File.Move(filepath + "\\" + filename, filepath + "\\" + newfilename);//上方如果判定为肯定，那就把路径名+旧名字filename改为路径名+新名字newfilenames

                        //string newfullpath = Path.Combine(filepath, newfilename);
                        try
                        {
                            File.Move(oldfullpath, newfullpath);

                        }
                        catch (IOException)
                        {
                            MessageBox.Show("无访问权限");

                            continue;
                        }
                        listBox1.Items[i] = newfullpath;
                    }
                    else
                    {
                        //此处
                        var result = MessageBox.Show("文件名重命名冲突");

                        //File.Move(Path.Combine(filepath + "\\" + filename, filepath + "\\" + newfilename),);
                        //int a = 1;

                        string newwithoutext = Path.GetFileNameWithoutExtension(newfilename);

                        string ext = Path.GetExtension(newfilename);

                        //newfilename = $"{newwithoutext}({a}){extension}";此处因为同一文件夹下修改固定文本，不可能产生多个重命名，所以不需要自动递增{a}，也不需要做while循环

                        newfilename = $"{newwithoutext}({1}){ext}";

                        newfullpath = Path.Combine(filepath, newfilename);

                        try
                        {
                            File.Move(oldfullpath, newfullpath);

                        }
                        catch (IOException)
                        {
                            MessageBox.Show("无访问权限");

                            continue;
                        }
                        listBox1.Items[i] = newfullpath;

                        MessageBox.Show("命名冲突文件已在后面增加（数字）后缀");
                    }


                }
            }
            MessageBox.Show("替换已完成");
        }

        private void button4_Click(object sender, EventArgs e)//选择文件夹下的所有文件
        {
            //DialogResult dialogResult1 = folderBrowserDialog1.ShowDialog();//创建一个枚举类对话框变量，然后把变量结果定义为dialogResult1

            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)  //判断这个枚举变量是否选择OK
            {
                textBox3.Text = folderBrowserDialog1.SelectedPath;

                string folderpath = folderBrowserDialog1.SelectedPath;

               

                DialogResult choice = MessageBox.Show("是否包含所有子文件夹下的文件", "选项", MessageBoxButtons.YesNo);

                try 
                {
                    if (choice == DialogResult.Yes)
                    {
                        string[] filepaths = Directory.GetFiles(folderpath, "*.*", SearchOption.AllDirectories);//遍历选择递归
                        foreach (string filepath in filepaths) //遍历所选择的所有filepaths里的数组，发牌员从"牌堆"filepaths中依次发出了“牌”filepath.接下来的{}中表达的要对发出的牌做的事
                        {
                            listBox1.Items.Add(filepath);//要对牌做的事：把每一张遍历到的filepath依次添加到listbox1列表

                        }

                    }
                    else
                    {
                        string[] filepaths = Directory.GetFiles(folderpath);//不递归
                        foreach (string filepath in filepaths) //遍历所选择的所有filepaths里的数组，发牌员从"牌堆"filepaths中依次发出了“牌”filepath.接下来的{}中表达的要对发出的牌做的事
                        {
                            listBox1.Items.Add(filepath);//要对牌做的事：把每一张遍历到的filepath依次添加到listbox1列表

                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("无权限访问部分子文件夹！");
                }

                catch (PathTooLongException)
                {
                    MessageBox.Show("路径过长，请缩短文件夹路径！");
                }

            }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)//统一修改所有文件的文件名
        {
             int a = 1;

             for (int i = 0; i < listBox1.Items.Count; i++)
             {
                
               
                string oldfullpath = listBox1.Items[i].ToString();

                string filepath = Path.GetDirectoryName(oldfullpath);

                string filename = Path.GetFileName(oldfullpath);

                string newwithoutext = Path.GetFileNameWithoutExtension(filename);

                string extension = Path.GetExtension(filename);



                string newfilename = filename.Replace(newwithoutext, textBox4.Text);

                newfilename = $"{newfilename}({a}){extension}";

                string newfullpath = Path.Combine(filepath, newfilename);

                a++;

                listBox1.Items[i] = newfullpath;

               
             }
            MessageBox.Show("文件名修改已完成");
        }

        private void button6_Click(object sender, EventArgs e)//PAGE2,选择需要整理的文件所在的文件夹
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)  //判断这个枚举变量是否选择OK
            {
                textBox5.Text = folderBrowserDialog1.SelectedPath;

                string folderpath = folderBrowserDialog1.SelectedPath;



                DialogResult choice = MessageBox.Show("是否包含所有子文件夹下的文件", "选项", MessageBoxButtons.YesNo);

                try
                {
                    if (choice == DialogResult.Yes)
                    {
                        string[] filepaths = Directory.GetFiles(folderpath, "*.*", SearchOption.AllDirectories);//遍历选择递归
                        foreach (string filepath in filepaths) //遍历所选择的所有filepaths里的数组，发牌员从"牌堆"filepaths中依次发出了“牌”filepath.接下来的{}中表达的要对发出的牌做的事
                        {
                            listBox2.Items.Add(filepath);//要对牌做的事：把每一张遍历到的filepath依次添加到listbox1列表

                        }

                    }
                    else
                    {
                        string[] filepaths = Directory.GetFiles(folderpath);//不递归
                        foreach (string filepath in filepaths) //遍历所选择的所有filepaths里的数组，发牌员从"牌堆"filepaths中依次发出了“牌”filepath.接下来的{}中表达的要对发出的牌做的事
                        {
                            listBox2.Items.Add(filepath);//要对牌做的事：把每一张遍历到的filepath依次添加到listbox1列表

                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("无权限访问部分子文件夹！");
                }

                catch (PathTooLongException)
                {
                    MessageBox.Show("路径过长，请缩短文件夹路径！");
                }

            }
        }

        private void button7_Click(object sender, EventArgs e)//page2，选择整理文件夹需要保存的目标文件夹
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK) 
            {
                textBox6.Text = folderBrowserDialog1.SelectedPath;
                string folderpath = folderBrowserDialog1.SelectedPath;
            }
            

        }

        private void button8_Click(object sender, EventArgs e)
        {

            string savefolderpath=textBox6.Text;
            //string savefilepath = Path.GetDirectoryName(savefolderpath);savefolderpath已经是纯目录了。

            

            for (int i = 0; i < listBox2.Items.Count; i++) 
            {
                string startfilepath = listBox2.Items[i].ToString();

                string startname = Path.GetFileName(startfilepath);
                string startpath = Path.GetDirectoryName(startfilepath);
                string withoutextname = Path.GetFileNameWithoutExtension(startfilepath);
                string ext = Path.GetExtension(startfilepath);

                
                
                //string newfullpath = Path.Combine (newfilepath, filename);

                string oldfullpath = Path.Combine (startpath, startname);


                string oldfullpath1 = Path.Combine(startpath, startname);//oldfulpath1是修改后缀名后的旧全路径
                string tempname = startname;
                if (checkBox1.Checked && startname.Contains(".skel.bytes")) 
                {
                    startname = startname.Replace(".skel.bytes", ".skel");

                    oldfullpath1 = Path.Combine(startpath, startname);//oldfulpath1是修改后缀名后的旧全路径

                    tempname = startname;
                    try
                    {
                        File.Move(oldfullpath, oldfullpath1);

                        withoutextname = Path.GetFileNameWithoutExtension (oldfullpath1);
                    }
                    catch(IOException) 
                    {
                        MessageBox.Show("无访问权限");

                        continue;
                    }
                    

                    ext = ".skel";
                }
                if (checkBox1.Checked && startname.Contains(".atlas.txt"))
                {
                    startname = startname.Replace(".atlas.txt", ".atlas");

                    oldfullpath1 = Path.Combine(startpath, startname);//oldfulpath1是修改后缀名后的旧全路径

                    tempname = startname;
                    try
                    {
                        File.Move(oldfullpath, oldfullpath1);

                        withoutextname = Path.GetFileNameWithoutExtension(oldfullpath1);
                    }
                    catch (IOException)
                    {
                        MessageBox.Show("无访问权限");

                        continue;
                    }


                    ext = ".atlas";
                }
                
                string targetpath = Path.Combine(savefolderpath, withoutextname);//定义变量，以无扩展名的文件命名

                if (!Directory.Exists(targetpath)) 
                {
                    Directory.CreateDirectory(targetpath);
                }

                string targetfullpath = Path.Combine(targetpath, startname);

                int a = 1;

                while (File.Exists(targetfullpath))
                {
                    tempname = $"{withoutextname}({a}){ext}";

                    targetfullpath = Path.Combine (targetpath, tempname);

                    //filename = newfilename;

                    a++;

                    if(a>=1)
                    {
                        var result = MessageBox.Show("修改过程中出现重名文件,已为名称重复的文件添加后缀");
                    }
                }
                try
                {
                    File.Move(oldfullpath1, targetfullpath);
                }
                catch (IOException)
                {
                    MessageBox.Show("无访问权限");

                    continue;
                }
                
                string  finalfullpath = Path.Combine(targetpath,tempname);
                string finalname = tempname;

                if (checkBox1.Checked && tempname.EndsWith(".skel"))
                {
                    finalname = tempname.Replace(".skel", ".skel.bytes");

                    finalfullpath = Path.Combine(targetpath, finalname);
                    try
                    {
                        File.Move(targetfullpath, finalfullpath);
                    }
                    catch (IOException)
                    {
                        MessageBox.Show("无访问权限");

                        continue;
                    }
                }
                if (checkBox1.Checked && tempname.EndsWith(".atlas"))
                {
                    finalname = tempname.Replace(".atlas", ".atlas.txt");

                    finalfullpath = Path.Combine(targetpath, finalname);
                    try
                    {
                        File.Move(targetfullpath, finalfullpath);
                    }
                    catch (IOException)
                    {
                        MessageBox.Show("无访问权限");

                        continue;
                    }
                }
            }
            MessageBox.Show("整理完成");   
        }//page2,整理文件

        private void button9_Click(object sender, EventArgs e)//page2,清空list
        {
            listBox2.Items.Clear();
        }

        private void button10_Click(object sender, EventArgs e)//PAGE3,选择需要解包图集的文件所在的文件夹
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)  //判断这个枚举变量是否选择OK
            {
                textBox7.Text = folderBrowserDialog1.SelectedPath;

                string folderpath = folderBrowserDialog1.SelectedPath;



                DialogResult choice = MessageBox.Show("是否包含所有子文件夹下的文件", "选项", MessageBoxButtons.YesNo);

                try
                {
                    if (choice == DialogResult.Yes)
                    {
                        listBox3.Items.Clear();

                        string[] atlasfilepaths = Directory.GetFiles(folderpath, "*.atlas*", SearchOption.AllDirectories);//遍历选择递归
                        foreach (string atlasfilepath in atlasfilepaths) //遍历所选择的所有filepaths里的数组，发牌员从"牌堆"filepaths中依次发出了“牌”filepath.接下来的{}中表达的要对发出的牌做的事
                        {
                            listBox3.Items.Add(atlasfilepath);//要对牌做的事：把每一张遍历到的filepath依次添加到listbox1列表

                        }

                    }
                    else
                    {
                        listBox3.Items.Clear();

                        string[] atlasfilepaths = Directory.GetFiles(folderpath,"*.atlas");//不递归
                        foreach (string atlasfilepath in atlasfilepaths) //遍历所选择的所有filepaths里的数组，发牌员从"牌堆"filepaths中依次发出了“牌”filepath.接下来的{}中表达的要对发出的牌做的事
                        {
                            listBox3.Items.Add(atlasfilepath);//要对牌做的事：把每一张遍历到的filepath依次添加到listbox1列表

                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("无权限访问部分子文件夹！");
                }

                catch (PathTooLongException)
                {
                    MessageBox.Show("路径过长，请缩短文件夹路径！");
                }

            }
        }

        private void button11_Click(object sender, EventArgs e)//page3清空list
        {
            listBox3.Items.Clear();
        }

        private void button13_Click(object sender, EventArgs e)//page3,开始解包
        {
            if (listBox3.Items.Count == 0) 
            {
                MessageBox.Show("请选择包含atlas和对应Png的文件夹！");
                return;
            }
            else 
            {
                foreach (string atlasfilepath in listBox3.Items) 
                {
                    string targetpath = Path.GetDirectoryName(atlasfilepath);
                    string imagespath = Path.Combine(targetpath, "images");
                    Directory.CreateDirectory(imagespath);

                    var regions = ParseAtlas(atlasfilepath);
                    MessageBox.Show($"{Path.GetFileName(atlasfilepath)} 解析到 {regions.Count} 个区域");
                    ExtractRegions(regions, targetpath, imagespath);
                }
            }
        }

        #region 核心导出逻辑
        class AtlasRegion 
        {
            public string Name;
            public int X, Y, Width, Height;
            public int Rotation;
            public string SourceImage;

            public int OrigWidth, OrigHeight;
            public int OffsetX, OffsetY;
            public int Index;

        }

        private static string MakeSafeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "unnamed.png";
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            name = name.Trim().TrimEnd('.');
            if (Path.HasExtension(name))
                name = Path.GetFileNameWithoutExtension(name);
            return name + ".png";
        }
        private List<AtlasRegion> ParseAtlas(string atlasFile)
        {
            var regions = new List<AtlasRegion>();
            string currentImage = null;
            var lines = File.ReadAllLines(atlasFile);
            int i = 0;

            while (i < lines.Length)
            {
                string raw = lines[i];
                if (string.IsNullOrWhiteSpace(raw)) { i++; continue; }

                if (!char.IsWhiteSpace(raw[0]))
                {
                    string line = raw.Trim();

                    if (line.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                        || line.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                        || line.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                    {
                        currentImage = Path.GetFileName(line);
                        i++;
                        continue;
                    }

                    if (line.Contains(":")) { i++; continue; }

                    string regionName = line;
                    int rotation = 0, x = 0, y = 0, w = 0, h = 0;
                    int origW = 0, origH = 0, offsetX = 0, offsetY = 0, index = -1;

                    i++;
                    while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]) && char.IsWhiteSpace(lines[i][0]))
                    {
                        string prop = lines[i].Trim();
                        int idx = prop.IndexOf(':');
                        if (idx >= 0)
                        {
                            string key = prop.Substring(0, idx).Trim();
                            string val = prop.Substring(idx + 1).Trim();
                            switch (key)
                            {
                                case "rotate":
                                    {
                                        var low = val.ToLower();
                                        if (low == "true") rotation = 90;
                                        else if (low == "false") rotation = 0;
                                        else if (int.TryParse(val, out int deg)) rotation = ((deg % 360) + 360) % 360;
                                        break;
                                    }
                                case "xy":
                                    {
                                        var parts = val.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                        if (parts.Length >= 2) { int.TryParse(parts[0], out x); int.TryParse(parts[1], out y); }
                                        break;
                                    }
                                case "size":
                                    {
                                        var parts = val.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                        if (parts.Length >= 2) { int.TryParse(parts[0], out w); int.TryParse(parts[1], out h); }
                                        break;
                                    }
                                case "orig":
                                    {
                                        var parts = val.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                        if (parts.Length >= 2) { int.TryParse(parts[0], out origW); int.TryParse(parts[1], out origH); }
                                        break;
                                    }
                                case "offset":
                                    {
                                        var parts = val.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                        if (parts.Length >= 2) { int.TryParse(parts[0], out offsetX); int.TryParse(parts[1], out offsetY); }
                                        break;
                                    }
                                case "index": int.TryParse(val, out index); break;
                            }
                        }
                        i++;
                    }

                    regions.Add(new AtlasRegion
                    {
                        Name = regionName,
                        X = x,
                        Y = y,
                        Width = w,
                        Height = h,
                        Rotation = rotation,
                        SourceImage = currentImage,
                        OrigWidth = origW,
                        OrigHeight = origH,
                        OffsetX = offsetX,
                        OffsetY = offsetY,
                        Index = index
                    });

                    continue;
                }

                i++;
            }

            return regions;
        }

        private void ExtractRegions(List<AtlasRegion> regions, string baseDir, string outDir)
        {
            var cache = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);

            foreach (var region in regions)
            {
                if (string.IsNullOrEmpty(region.SourceImage))
                {
                    Console.WriteLine($"跳过 {region.Name}：没有 source image");
                    continue;
                }

                if (!cache.TryGetValue(region.SourceImage, out Bitmap src))
                {
                    string pngPath = Path.Combine(baseDir, region.SourceImage);
                    if (!File.Exists(pngPath))
                    {
                        var matches = Directory.GetFiles(baseDir, "*.*", SearchOption.TopDirectoryOnly)
                                              .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                                                       || f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                                                       || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                                              .ToArray();
                        string found = matches.FirstOrDefault(f => Path.GetFileName(f).Equals(region.SourceImage, StringComparison.OrdinalIgnoreCase));
                        if (found != null) pngPath = found;
                    }

                    if (!File.Exists(pngPath))
                    {
                        Console.WriteLine($"找不到图集文件 {region.SourceImage} (baseDir: {baseDir})，已跳过该 region。");
                        continue;
                    }

                    src = new Bitmap(pngPath);
                    cache[region.SourceImage] = src;
                }

                if (region.Width <= 0 || region.Height <= 0
                    || region.X < 0 || region.Y < 0
                    || region.X + region.Width > src.Width
                    || region.Y + region.Height > src.Height)
                {
                    Console.WriteLine($"跳过 {region.Name}：尺寸或位置无效");
                    continue;
                }

                // 从大图裁出
                var rect = new Rectangle(region.X, region.Y, region.Width, region.Height);
                Bitmap piece = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(piece))
                {
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                    g.DrawImage(src, new Rectangle(0, 0, piece.Width, piece.Height), rect, GraphicsUnit.Pixel);
                }
                // 说明：
                //  - region.Rotation 表示 atlas 中记录的“被旋转的角度”（ParseAtlas 已解析为 0/90/180/270）
                //  - 要把 piece 恢复到原始方向，应将其旋转 (360 - region.Rotation) 度（顺时针方向）
                //  - 使用 RotateFlip 在原图上就地执行旋转（简单可靠）

                int rot = ((region.Rotation % 360) + 360) % 360;
                if (rot != 0)
                {
                    
                    switch (rot)
                    {
                        case 90:
                            piece.RotateFlip(RotateFlipType.Rotate90FlipNone);
                            break;
                        case 180:
                            piece.RotateFlip(RotateFlipType.Rotate180FlipNone);
                            break;
                        case 270:
                            piece.RotateFlip(RotateFlipType.Rotate270FlipNone);
                            break;
                        default:
                            // 非90倍数（理论上 atlas 只会是 0/90/180/270），若出现非标准值，忽略
                            break;
                    }
                }
                // note: 此时 `piece` 已被就地旋转回“原始方向”，大小也会相应交换（如果是 90/270）

                // 计算最终目标画布大小（使用 orig 信息优先）
                int finalW = (region.OrigWidth > 0) ? region.OrigWidth : piece.Width;
                int finalH = (region.OrigHeight > 0) ? region.OrigHeight : piece.Height;

                using (var finalBmp = new Bitmap(finalW, finalH, PixelFormat.Format32bppArgb))
                using (var g3 = Graphics.FromImage(finalBmp))
                {
                    g3.Clear(Color.Transparent);
                    g3.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                    // 重要修正：atlas 文件中的 offset 含义是 "从原图的左侧和底部裁去的空白像素数量"
                    // 因此 X 可以直接作为左侧偏移量，但 Y 是从底部开始计算，需要转换为从顶部的坐标：
                    // destY = OrigHeight - offsetY - spriteHeight
                    int destX = region.OffsetX;
                    int destY;
                    if (region.OrigHeight > 0)
                    {
                        // offset.Y 是从底部算的空白，需要转换为 GDI 的从顶端计数：
                        destY = region.OrigHeight - region.OffsetY - piece.Height;
                    }
                    else
                    {
                        // 没有 orig 信息时保守放置（与之前行为一致）
                        destY = region.OffsetY;
                    }

                    // 防止负值（避免 DrawImage 抛错或把图放出边界）
                    if (destX < 0) destX = 0;
                    if (destY < 0) destY = 0;

                    // ======= 关键修正点：以前你直接用了 region.OffsetX/OffsetY，这里改为使用 destX/destY =======
                    g3.DrawImage(piece, destX, destY);

                    string outName = MakeSafeFileName(region.Name);
                    string outPath = Path.Combine(outDir, outName);
                    finalBmp.Save(outPath, ImageFormat.Png);
                }

                // 释放 piece（我们用的是就地旋转，所以直接释放）
                piece.Dispose();
            }

            foreach (var b in cache.Values) b.Dispose();
        }

        #endregion
    }
}
