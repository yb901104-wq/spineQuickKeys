using System.Reflection;
using KeyMacro.Controls;
using KeyMacro.Forms;
using KeyMacro.Forms.ReNameTool;

namespace Audit027ScreenshotHarness;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", ".."));
        var screenshotDir = Path.Combine(repoRoot, "docs", "verification", "screenshots");
        Directory.CreateDirectory(screenshotDir);

        using (var cli = new BatchCliWindow())
        {
            InvokePrivate(cli, "ShowProgress", true);
            InvokePrivate(cli, "SetProgress", 2, 5, "Experimental merge: G5_target_copy.spine");
            SaveForm(cli, Path.Combine(screenshotDir, "AUD-027_cli-progress-layout.png"));
        }

        using (var copy = new BatchCopyWindow())
        {
            InvokePrivate(copy, "SetProgress", 4, 10, "Copying: zzps-skin.json -> targets4");
            SaveForm(copy, Path.Combine(screenshotDir, "AUD-027_batch-copy-progress-layout.png"));
        }

        using (var rename = new Form1())
        {
            foreach (var bar in FindControls<TextProgressBar>(rename))
            {
                bar.Maximum = 8;
                bar.Value = 3;
                bar.ProgressText = "3/8";
            }

            foreach (var label in FindControls<Label>(rename)
                .Where(l => l.Width >= 450 && l.Height <= 22 && string.IsNullOrEmpty(l.Text)))
            {
                label.Text = "Processing: G5.skel.bytes";
            }

            var tabs = FindControls<TabControl>(rename).First();
            tabs.SelectedIndex = 0;
            SaveForm(rename, Path.Combine(screenshotDir, "AUD-027_rename-tool-progress-layout.png"), closeAfterSave: false);
            tabs.SelectedIndex = 1;
            SaveForm(rename, Path.Combine(screenshotDir, "AUD-027_rename-tool-organize-progress-layout.png"), closeAfterSave: false);
            tabs.SelectedIndex = 2;
            SaveForm(rename, Path.Combine(screenshotDir, "AUD-027_rename-tool-unpack-progress-layout.png"));
        }
    }

    private static void InvokePrivate(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(target.GetType().FullName, methodName);
        method.Invoke(target, args);
    }

    private static IEnumerable<T> FindControls<T>(Control root) where T : Control
    {
        foreach (Control child in root.Controls)
        {
            if (child is T typed) yield return typed;
            foreach (var nested in FindControls<T>(child))
                yield return nested;
        }
    }

    private static void SaveForm(Form form, string path, bool closeAfterSave = true)
    {
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(40, 40);
        form.Show();
        Application.DoEvents();
        Thread.Sleep(250);

        using var bmp = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        if (closeAfterSave)
            form.Close();

        Console.WriteLine(path);
    }
}
