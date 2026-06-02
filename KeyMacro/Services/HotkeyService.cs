using System.Diagnostics;
using System.Runtime.InteropServices;
using KeyMacro.Models;

namespace KeyMacro.Services;

public sealed class HotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly IntPtr _hWnd;
    private readonly Dictionary<int, string> _hotkeyMap = [];
    private readonly Dictionary<string, TargetAppSpec> _targetApps = [];
    private volatile bool _paused;
    private int _nextId = 1;

    public event Action<string>? HotkeyTriggered;
    public bool IsPaused => _paused;

    public HotkeyService(IntPtr windowHandle) => _hWnd = windowHandle;

    public List<string> RegisterAll(IEnumerable<MacroSequence> sequences)
    {
        var failed = new List<string>();
        UnregisterAll();

        int registeredCount = 0;
        foreach (var seq in sequences)
        {
            _targetApps[seq.Id] = TargetAppSpec.FromSequence(seq);

            if (!seq.Enabled || string.IsNullOrWhiteSpace(seq.TriggerHotkey))
                continue;

            if (TryParseHotkey(seq.TriggerHotkey, out var mod, out var vk))
            {
                if (RegisterHotKey(_hWnd, _nextId, mod | MOD_NOREPEAT, vk))
                {
                    _hotkeyMap[_nextId] = seq.Id;
                    _nextId++;
                    registeredCount++;
                }
                else
                {
                    failed.Add(seq.TriggerHotkey);
                }
            }
            else
            {
                failed.Add(seq.TriggerHotkey);
            }
        }

        if (failed.Count > 0)
            OperationLogger.Warn($"HotkeyService.RegisterAll: registered {registeredCount}, failed {failed.Count}: {string.Join(", ", failed)}");
        else
            OperationLogger.Info($"HotkeyService.RegisterAll: registered {registeredCount} hotkeys, 0 failed");
        return failed;
    }

    public void UnregisterAll()
    {
        foreach (var id in _hotkeyMap.Keys)
            UnregisterHotKey(_hWnd, id);
        _hotkeyMap.Clear();
        _targetApps.Clear();
    }

    public bool HandleWindowMessage(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && !_paused)
        {
            int id = m.WParam.ToInt32();
            if (_hotkeyMap.TryGetValue(id, out var seqId))
            {
                if (_targetApps.TryGetValue(seqId, out var target) &&
                    target.HasTarget &&
                    !IsForegroundTarget(target))
                    return false;

                OperationLogger.Info($"HotkeyService: triggered seqId={seqId}");
                HotkeyTriggered?.Invoke(seqId);
                return true;
            }
        }
        return false;
    }

    public void SetPaused(bool paused) => _paused = paused;
    public void Dispose() => UnregisterAll();

    private static bool IsForegroundTarget(TargetAppSpec target)
    {
        var hwnd = GetForegroundWindow();
        GetWindowThreadProcessId(hwnd, out var pid);
        try
        {
            using var proc = Process.GetProcessById((int)pid);
            if (!string.IsNullOrEmpty(target.ProcessName) &&
                string.Equals(proc.ProcessName, target.ProcessName, StringComparison.OrdinalIgnoreCase))
                return true;

            var fgPath = proc.MainModule?.FileName ?? "";
            return !string.IsNullOrEmpty(target.Path) &&
                string.Equals(fgPath, target.Path, StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    private sealed record TargetAppSpec(string Path, string ProcessName)
    {
        public bool HasTarget => !string.IsNullOrWhiteSpace(Path) || !string.IsNullOrWhiteSpace(ProcessName);

        public static TargetAppSpec FromSequence(MacroSequence seq)
        {
            var processName = seq.TargetAppProcessName;
            if (string.IsNullOrWhiteSpace(processName) && !string.IsNullOrWhiteSpace(seq.TargetAppPath))
                processName = System.IO.Path.GetFileNameWithoutExtension(seq.TargetAppPath);
            return new TargetAppSpec(seq.TargetAppPath, processName);
        }
    }

    private static bool TryParseHotkey(string hotkey, out uint modifiers, out uint vk)
    {
        modifiers = 0;
        vk = 0;

        var parts = hotkey.Split('+', StringSplitOptions.TrimEntries);
        if (parts.Length < 2) return false;

        var keyPart = parts[^1];
        for (int i = 0; i < parts.Length - 1; i++)
        {
            modifiers |= parts[i].ToLower() switch
            {
                "ctrl" => MOD_CONTROL,
                "alt" => MOD_ALT,
                "shift" => MOD_SHIFT,
                "win" => MOD_WIN,
                _ => 0
            };
        }

        if (modifiers == 0) return false;
        if (!Enum.TryParse<Keys>(keyPart, true, out var key)) return false;
        vk = (uint)key;
        return true;
    }

    public static string FormatHotkey(Keys keyCode, bool ctrl, bool alt, bool shift, bool win)
    {
        var parts = new List<string>();
        if (ctrl) parts.Add("Ctrl");
        if (alt) parts.Add("Alt");
        if (shift) parts.Add("Shift");
        if (win) parts.Add("Win");

        if (keyCode is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin
            or Keys.LControlKey or Keys.RControlKey or Keys.LShiftKey or Keys.RShiftKey)
            return "";

        parts.Add(keyCode.ToString());
        return string.Join("+", parts);
    }
}
