using KeyMacro.Models;

namespace KeyMacro.Services;

public class VirtualLoopExecutor
{
    private readonly Dictionary<string, CancellationTokenSource> _activeLoops = [];
    private readonly MacroPlayer _player;

    public VirtualLoopExecutor(MacroPlayer player)
    {
        _player = player;
    }

    public bool IsLooping(string buttonId) => _activeLoops.ContainsKey(buttonId);

    public async void StartLoop(VirtualButton vbtn, MacroSequence sequence)
    {
        if (_activeLoops.ContainsKey(vbtn.Id)) return;

        var cts = new CancellationTokenSource();
        _activeLoops[vbtn.Id] = cts;

        try
        {
            for (int i = 0; i < vbtn.LoopCount; i++)
            {
                if (cts.Token.IsCancellationRequested) break;
                await _player.Play(sequence);
                if (i < vbtn.LoopCount - 1)
                    await Task.Delay(vbtn.LoopInterval, cts.Token);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _activeLoops.Remove(vbtn.Id);
        }
    }

    public void StopLoop(string buttonId)
    {
        if (_activeLoops.TryGetValue(buttonId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
            _activeLoops.Remove(buttonId);
        }
    }

    public void StopAll()
    {
        foreach (var (id, cts) in _activeLoops.ToArray())
        {
            cts.Cancel();
            cts.Dispose();
            _activeLoops.Remove(id);
        }
    }
}
