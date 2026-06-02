using System.Diagnostics;
using System.Runtime.InteropServices;

namespace KeyMacro;

static class Program
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_RESTORE = 9;

    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, @"Global\KeyMacro_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            var hWnd = FindExistingMainWindow();
            if (hWnd != IntPtr.Zero)
            {
                ShowWindow(hWnd, SW_RESTORE);
                SetForegroundWindow(hWnd);
            }
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        ApplicationConfiguration.Initialize();
        Application.Run(new Forms.MainForm());
    }

    private static IntPtr FindExistingMainWindow()
    {
        try
        {
            using var current = Process.GetCurrentProcess();
            return Process.GetProcessesByName(current.ProcessName)
                .Where(p => p.Id != current.Id)
                .Select(p =>
                {
                    try { return p.MainWindowHandle; }
                    finally { p.Dispose(); }
                })
                .FirstOrDefault(h => h != IntPtr.Zero);
        }
        catch
        {
            return IntPtr.Zero;
        }
    }
}
