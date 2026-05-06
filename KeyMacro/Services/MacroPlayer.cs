using KeyMacro.Models;

namespace KeyMacro.Services;

public class MacroPlayer
{
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

            do
            {
                foreach (var step in sequence.Steps)
                {
                    ct.ThrowIfCancellationRequested();

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

                    if (step.DelayMs > 0)
                        await Task.Delay(step.DelayMs, ct);
                }

                if (sequence.Loop && !ct.IsCancellationRequested)
                    await Task.Delay(sequence.LoopIntervalMs, ct);
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

        SendKeys.SendWait(special.Contains(upper) ? "{" + upper + "}" : key);
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
