using System.Runtime.InteropServices;

namespace KeyMacro.Services;

public static class NativeWindowTheme
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;

    public static void ApplyDarkTitleBar(Form form)
    {
        if (form.IsDisposed)
            return;

        if (form.IsHandleCreated)
        {
            TryApply(form.Handle);
            return;
        }

        form.HandleCreated += (_, _) => TryApply(form.Handle);
    }

    private static void TryApply(IntPtr handle)
    {
        if (handle == IntPtr.Zero || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
            return;

        var enabled = 1;
        _ = DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref enabled, sizeof(int));
        _ = DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkModeBefore20H1, ref enabled, sizeof(int));
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int pvAttribute, int cbAttribute);
}
