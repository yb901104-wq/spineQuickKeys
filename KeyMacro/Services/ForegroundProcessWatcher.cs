using System.Runtime.InteropServices;

namespace KeyMacro.Services;

/// <summary>Receives foreground-window changes without enumerating all processes.</summary>
public sealed class ForegroundProcessWatcher : IDisposable
{
    private const uint EventSystemForeground = 0x0003;
    private const uint WineventOutOfContext = 0x0000;

    private delegate void WinEventDelegate(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint idEventThread,
        uint msEventTime);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWinEventHook(
        uint eventMin,
        uint eventMax,
        IntPtr hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc,
        uint idProcess,
        uint idThread,
        uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    private readonly WinEventDelegate _callback;
    private IntPtr _hook;
    private bool _disposed;

    public event Action<IntPtr>? ForegroundChanged;

    public ForegroundProcessWatcher()
    {
        _callback = OnWinEvent;
    }

    public bool Start()
    {
        if (_disposed || _hook != IntPtr.Zero) return _hook != IntPtr.Zero;
        _hook = SetWinEventHook(
            EventSystemForeground,
            EventSystemForeground,
            IntPtr.Zero,
            _callback,
            0,
            0,
            WineventOutOfContext);
        OperationLogger.Info($"ForegroundProcessWatcher.Start: success={_hook != IntPtr.Zero}");
        return _hook != IntPtr.Zero;
    }

    private void OnWinEvent(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint idEventThread,
        uint msEventTime)
    {
        if (_disposed || eventType != EventSystemForeground || hwnd == IntPtr.Zero) return;
        ForegroundChanged?.Invoke(hwnd);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_hook != IntPtr.Zero)
        {
            UnhookWinEvent(_hook);
            _hook = IntPtr.Zero;
        }
        ForegroundChanged = null;
        GC.SuppressFinalize(this);
    }
}
