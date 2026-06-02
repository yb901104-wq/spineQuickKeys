using KeyMacro.Models;

namespace KeyMacro.Services;

public class VirtualLoopExecutor
{
    private readonly Dictionary<string, LoopState> _activeLoops = [];
    private readonly MacroPlayer _player;

    public event Action<string>? LoopEnded;

    public VirtualLoopExecutor(MacroPlayer player)
    {
        _player = player;
    }

    public bool IsLooping(string buttonId) => _activeLoops.ContainsKey(buttonId);

    public async void StartLoop(VirtualButton vbtn, MacroSequence sequence)
    {
        if (_activeLoops.ContainsKey(vbtn.Id)) return;

        var state = new LoopState();
        _activeLoops[vbtn.Id] = state;

        try
        {
            var loopIndex = 0;
            while (!state.StopRequested && !state.ForceStopped)
            {
                if (vbtn.LoopCount > 0 && loopIndex >= vbtn.LoopCount) break;
                loopIndex++;

                await _player.Play(sequence);

                if (state.StopRequested || state.ForceStopped) break;
                if (vbtn.LoopCount > 0 && loopIndex >= vbtn.LoopCount) break;

                while (state.MenuPaused && !state.StopRequested && !state.ForceStopped)
                    await Task.Delay(50, state.Cts.Token);

                if (state.StopRequested || state.ForceStopped) break;
                await Task.Delay(vbtn.LoopInterval, state.Cts.Token);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _activeLoops.Remove(vbtn.Id);
            state.Cts.Dispose();
            LoopEnded?.Invoke(vbtn.Id);
        }
    }

    public void StopLoop(string buttonId)
    {
        if (_activeLoops.TryGetValue(buttonId, out var state))
            state.StopRequested = true;
    }

    public void PauseForMenu(string buttonId)
    {
        if (_activeLoops.TryGetValue(buttonId, out var state))
            state.MenuPaused = true;
    }

    public void ResumeFromMenu(string buttonId)
    {
        if (_activeLoops.TryGetValue(buttonId, out var state))
            state.MenuPaused = false;
    }

    public void ForceStopLoop(string buttonId)
    {
        if (_activeLoops.TryGetValue(buttonId, out var state))
        {
            state.ForceStopped = true;
            state.Cts.Cancel();
            _player.ForceStop();
        }
    }

    public void StopAll()
    {
        foreach (var (id, state) in _activeLoops.ToArray())
        {
            state.ForceStopped = true;
            state.Cts.Cancel();
            _activeLoops.Remove(id);
        }
        _player.ForceStop();
    }

    private sealed class LoopState
    {
        public CancellationTokenSource Cts { get; } = new();
        public bool StopRequested { get; set; }
        public bool ForceStopped { get; set; }
        public bool MenuPaused { get; set; }
    }
}
