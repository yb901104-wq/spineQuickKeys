using System.Runtime.InteropServices;
using KeyMacro.Models;

namespace KeyMacro.Services;

public class MacroPlayer
{
    private const uint KEYEVENTF_KEYDOWN = 0x0000;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern IntPtr PostMessage(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    private const uint WM_KEYDOWN = 0x0100;
    private const uint WM_KEYUP = 0x0101;
    private const uint WM_CHAR = 0x0102;
    private const uint MAPVK_VK_TO_VSC = 0;

    private volatile bool _isPlaying;
    private CancellationTokenSource? _cts;

    public bool IsPlaying => _isPlaying;

    public async Task Play(MacroSequence sequence)
    {
        if (_isPlaying) return;

        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        _isPlaying = true;
        try
        {
            await Task.Delay(500, ct);

            var loopCounter = 0;
            do
            {
                loopCounter++;
                foreach (var step in sequence.Steps)
                {
                    ct.ThrowIfCancellationRequested();

                    if (step.PressMode == PressMode.Hold && step.Type != StepType.Text)
                    {
                        await PlayHold(step, ct);
                    }
                    else
                    {
                        switch (step.Type)
                        {
                            case StepType.Key:
                                SendKey(step.Keys);
                                break;
                            case StepType.Combo:
                                SendCombo(step.Keys);
                                break;
                            case StepType.Text:
                                SendText(step.Keys);
                                break;
                        }
                    }

                    if (step.DelayMs > 0)
                        await Task.Delay(step.DelayMs, ct);
                }

                if (sequence.Loop && !ct.IsCancellationRequested)
                {
                    if (sequence.LoopCount > 0 && loopCounter >= sequence.LoopCount)
                        break;
                    await Task.Delay(sequence.LoopIntervalMs, ct);
                }
            }
            while (sequence.Loop && !ct.IsCancellationRequested);
        }
        catch (OperationCanceledException) { }
        finally
        {
            _isPlaying = false;
        }
    }

    public void Stop() => _cts?.Cancel();

    // ── PlayToWindow: PostMessage-based playback, no activation needed ──

    private static IntPtr MakeLParam(byte vk, bool keyUp)
    {
        var scan = (uint)MapVirtualKey(vk, MAPVK_VK_TO_VSC);
        uint lParam = 1;                    // repeat count
        lParam |= (scan & 0xFF) << 16;      // scan code
        // bit 24: extended key flag (set for extended keys)
        if (IsExtendedKey(vk))
            lParam |= 0x01000000;
        if (keyUp)
            lParam |= 0xC0000000;           // bit 30 (prev state) + bit 31 (transition)
        return (IntPtr)lParam;
    }

    private static bool IsExtendedKey(byte vk)
    {
        return vk is (byte)Keys.RMenu or (byte)Keys.RControlKey
            or (byte)Keys.Left or (byte)Keys.Right or (byte)Keys.Up or (byte)Keys.Down
            or (byte)Keys.Home or (byte)Keys.End or (byte)Keys.PageUp or (byte)Keys.PageDown
            or (byte)Keys.Insert or (byte)Keys.Delete
            or (byte)Keys.LWin or (byte)Keys.RWin
            or (byte)Keys.PrintScreen;
    }

    public async Task PlayToWindow(MacroSequence sequence, IntPtr targetHwnd)
    {
        if (_isPlaying) return;

        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        _isPlaying = true;
        try
        {
            var loopCounter = 0;
            do
            {
                loopCounter++;
                foreach (var step in sequence.Steps)
                {
                    ct.ThrowIfCancellationRequested();

                    if (step.PressMode == PressMode.Hold && step.Type != StepType.Text)
                    {
                        await PlayHoldToWindow(step, targetHwnd, ct);
                    }
                    else
                    {
                        switch (step.Type)
                        {
                            case StepType.Key:
                                PostKey(targetHwnd, step.Keys);
                                break;
                            case StepType.Combo:
                                PostCombo(targetHwnd, step.Keys);
                                break;
                            case StepType.Text:
                                PostText(targetHwnd, step.Keys);
                                break;
                        }
                    }

                    if (step.DelayMs > 0)
                        await Task.Delay(step.DelayMs, ct);
                }

                if (sequence.Loop && !ct.IsCancellationRequested)
                {
                    if (sequence.LoopCount > 0 && loopCounter >= sequence.LoopCount)
                        break;
                    await Task.Delay(sequence.LoopIntervalMs, ct);
                }
            }
            while (sequence.Loop && !ct.IsCancellationRequested);
        }
        catch (OperationCanceledException) { }
        finally
        {
            _isPlaying = false;
        }
    }

    private static async Task PlayHoldToWindow(MacroStep step, IntPtr hWnd, CancellationToken ct)
    {
        var duration = step.HoldDurationMs > 0 ? step.HoldDurationMs : 500;

        if (step.Type == StepType.Combo)
        {
            var (modifiers, keyVk) = ParseCombo(step.Keys);
            if (keyVk == 0) return;

            foreach (var mod in modifiers)
                PostMessage(hWnd, WM_KEYDOWN, (UIntPtr)mod, MakeLParam(mod, false));

            PostMessage(hWnd, WM_KEYDOWN, (UIntPtr)keyVk, MakeLParam(keyVk, false));
            await Task.Delay(duration, ct);
            PostMessage(hWnd, WM_KEYUP, (UIntPtr)keyVk, MakeLParam(keyVk, true));

            for (int i = modifiers.Length - 1; i >= 0; i--)
                PostMessage(hWnd, WM_KEYUP, (UIntPtr)modifiers[i], MakeLParam(modifiers[i], true));
        }
        else
        {
            if (!TryGetVk(step.Keys, out var vk)) return;
            PostMessage(hWnd, WM_KEYDOWN, (UIntPtr)vk, MakeLParam(vk, false));
            await Task.Delay(duration, ct);
            PostMessage(hWnd, WM_KEYUP, (UIntPtr)vk, MakeLParam(vk, true));
        }
    }

    private static void PostKey(IntPtr hWnd, string key)
    {
        if (key.Length == 1 && !char.IsLetter(key[0]))
        {
            PostMessage(hWnd, WM_CHAR, (UIntPtr)key[0], IntPtr.Zero);
            return;
        }

        var upper = key.ToUpper();
        if (Enum.TryParse<Keys>(upper, true, out var keys))
        {
            var vk = (byte)keys;
            PostMessage(hWnd, WM_KEYDOWN, (UIntPtr)vk, MakeLParam(vk, false));
            PostMessage(hWnd, WM_KEYUP, (UIntPtr)vk, MakeLParam(vk, true));
        }
    }

    private static void PostCombo(IntPtr hWnd, string combo)
    {
        var parts = combo.Split('+', StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            PostKey(hWnd, combo);
            return;
        }

        var keyStr = parts[^1];
        if (!Enum.TryParse<Keys>(keyStr, true, out var key)) return;
        var keyVk = (byte)key;

        // Press modifiers
        var modVks = new List<byte>();
        foreach (var mod in parts.Take(parts.Length - 1))
        {
            var modVk = mod.ToLower() switch
            {
                "ctrl" => (byte)Keys.ControlKey,
                "alt" => (byte)Keys.Menu,
                "shift" => (byte)Keys.ShiftKey,
                "win" => (byte)Keys.LWin,
                _ => (byte)0
            };
            if (modVk != 0)
            {
                modVks.Add(modVk);
                PostMessage(hWnd, WM_KEYDOWN, (UIntPtr)modVk, MakeLParam(modVk, false));
            }
        }

        // Press and release main key
        PostMessage(hWnd, WM_KEYDOWN, (UIntPtr)keyVk, MakeLParam(keyVk, false));
        PostMessage(hWnd, WM_KEYUP, (UIntPtr)keyVk, MakeLParam(keyVk, true));

        // Release modifiers in reverse
        for (int i = modVks.Count - 1; i >= 0; i--)
            PostMessage(hWnd, WM_KEYUP, (UIntPtr)modVks[i], MakeLParam(modVks[i], true));
    }

    private static void PostText(IntPtr hWnd, string text)
    {
        foreach (char c in text)
            PostMessage(hWnd, WM_CHAR, (UIntPtr)c, IntPtr.Zero);
    }

    private static async Task PlayHold(MacroStep step, CancellationToken ct)
    {
        var duration = step.HoldDurationMs > 0 ? step.HoldDurationMs : 500;

        if (step.Type == StepType.Combo)
        {
            var (modifiers, keyVk) = ParseCombo(step.Keys);
            if (keyVk == 0) return;

            foreach (var mod in modifiers)
                keybd_event(mod, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);

            keybd_event(keyVk, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            await Task.Delay(duration, ct);
            keybd_event(keyVk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            for (int i = modifiers.Length - 1; i >= 0; i--)
                keybd_event(modifiers[i], 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
        else
        {
            if (!TryGetVk(step.Keys, out var vk)) return;
            keybd_event(vk, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            await Task.Delay(duration, ct);
            keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
    }

    private static (byte[] modifiers, byte keyVk) ParseCombo(string combo)
    {
        var parts = combo.Split('+', StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            TryGetVk(combo, out var vk);
            return ([], vk);
        }

        var modifiers = new List<byte>();
        foreach (var mod in parts.Take(parts.Length - 1))
        {
            var vk = mod.ToLower() switch
            {
                "ctrl" => (byte)Keys.ControlKey,
                "alt" => (byte)Keys.Menu,
                "shift" => (byte)Keys.ShiftKey,
                "win" => (byte)Keys.LWin,
                _ => (byte)0
            };
            if (vk != 0) modifiers.Add(vk);
        }

        TryGetVk(parts[^1], out var keyVk);
        return (modifiers.ToArray(), keyVk);
    }

    private static bool TryGetVk(string key, out byte vk)
    {
        vk = 0;
        if (string.IsNullOrEmpty(key)) return false;

        if (Enum.TryParse<Keys>(key, true, out var keys))
        {
            vk = (byte)keys;
            return true;
        }
        return false;
    }

    private static void SendKey(string key)
    {
        if (key.Length == 1 && !char.IsLetter(key[0]))
        {
            SendKeys.SendWait(key);
            return;
        }

        var upper = key.ToUpper();
        var special = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ENTER", "TAB", "ESC", "ESCAPE", "BACK", "BACKSPACE", "DELETE", "DEL",
            "INSERT", "HOME", "END", "PGUP", "PAGEUP", "PGDN", "PAGEDOWN",
            "LEFT", "RIGHT", "UP", "DOWN", "SPACE", "CAPSLOCK",
            "F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12",
            "F13","F14","F15","F16","F17","F18","F19","F20","F21","F22","F23","F24",
            "NUMLOCK", "SCROLLLOCK", "PRINTSCREEN", "BREAK", "PAUSE"
        };

        SendKeys.SendWait(special.Contains(upper) ? "{" + upper + "}" :
            key.Length == 1 && char.IsLetter(key[0]) ? key.ToLowerInvariant() : key);
    }

    private static void SendCombo(string combo)
    {
        var parts = combo.Split('+', StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            SendKey(combo);
            return;
        }

        var prefix = "";
        var key = parts[^1];

        foreach (var mod in parts.Take(parts.Length - 1))
        {
            prefix += mod.ToLower() switch
            {
                "ctrl" => "^",
                "alt" => "%",
                "shift" => "+",
                "win" => "^",
                _ => ""
            };
        }

        var special = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ENTER", "TAB", "ESC", "ESCAPE", "BACK", "BACKSPACE", "DELETE", "DEL",
            "INSERT", "HOME", "END", "PGUP", "PAGEUP", "PGDN", "PAGEDOWN",
            "LEFT", "RIGHT", "UP", "DOWN", "SPACE",
            "F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12",
            "F13","F14","F15","F16","F17","F18","F19","F20","F21","F22","F23","F24"
        };

        if (special.Contains(key) || key.Length > 1)
            SendKeys.SendWait(prefix + "{" + key.ToUpper() + "}");
        else
            SendKeys.SendWait(prefix + key);
    }

    private static void SendText(string text)
    {
        foreach (char c in text)
        {
            string s = c.ToString();
            switch (c)
            {
                case '+': SendKeys.SendWait("{+}"); break;
                case '^': SendKeys.SendWait("{^}"); break;
                case '%': SendKeys.SendWait("{%}"); break;
                case '~': SendKeys.SendWait("{~}"); break;
                case '{': SendKeys.SendWait("{{}"); break;
                case '}': SendKeys.SendWait("{}}"); break;
                default: SendKeys.SendWait(s); break;
            }
        }
    }
}
