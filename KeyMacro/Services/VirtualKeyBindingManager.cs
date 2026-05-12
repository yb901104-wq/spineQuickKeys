using KeyMacro.Models;

namespace KeyMacro.Services;

public class VirtualKeyBindingManager
{
    private readonly HotkeyService _hotkeyService;
    private readonly VirtualButtonManager _buttonManager;

    public VirtualKeyBindingManager(HotkeyService hotkeyService, VirtualButtonManager buttonManager)
    {
        _hotkeyService = hotkeyService;
        _buttonManager = buttonManager;
    }

    public bool TryBind(VirtualButton vbtn, string sequenceId)
    {
        // Check conflict: another virtual button already bound to this sequence
        if (_buttonManager.IsIdBound(sequenceId))
            return false;

        vbtn.BindActionId = sequenceId;
        return true;
    }

    public void Unbind(VirtualButton vbtn)
    {
        vbtn.BindActionId = null;
    }

    public MacroSequence? ResolveBinding(VirtualButton vbtn, List<MacroSequence> sequences)
    {
        if (string.IsNullOrEmpty(vbtn.BindActionId)) return null;
        return sequences.Find(s => s.Id == vbtn.BindActionId);
    }
}
