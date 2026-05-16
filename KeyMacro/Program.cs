using System.Runtime.InteropServices;

namespace KeyMacro;

static class Program
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, @"Global\KeyMacro_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            var hWnd = FindWindow(null, "Spine助手 V2.12");
            if (hWnd != IntPtr.Zero)
                SetForegroundWindow(hWnd);
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        ApplicationConfiguration.Initialize();
        Application.Run(new Forms.MainForm());
    }
}
