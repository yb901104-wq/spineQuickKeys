using KeyMacro.Models;

namespace KeyMacro.Services;

public class VirtualButtonManager
{
    private readonly List<VirtualButton> _buttons = [];
    private int _nextIndex = 1;

    public IReadOnlyList<VirtualButton> Buttons => _buttons;

    public event Action? ButtonsChanged;

    public VirtualButton AddButton(VirtualButtonStyle style = VirtualButtonStyle.SmallIcon)
    {
        var btn = new VirtualButton
        {
            Name = $"按钮{_nextIndex++}",
            StyleType = style
        };
        _buttons.Add(btn);
        ButtonsChanged?.Invoke();
        return btn;
    }

    public void RemoveButton(string id)
    {
        _buttons.RemoveAll(b => b.Id == id);
        ButtonsChanged?.Invoke();
    }

    public void RemoveLast()
    {
        if (_buttons.Count > 0)
        {
            _buttons.RemoveAt(_buttons.Count - 1);
            ButtonsChanged?.Invoke();
        }
    }

    public void Clear()
    {
        _buttons.Clear();
        _nextIndex = 1;
        ButtonsChanged?.Invoke();
    }

    public VirtualButton? Find(string id) => _buttons.Find(b => b.Id == id);
    public VirtualButton? FindByName(string name) => _buttons.Find(b => b.Name == name);

    public void UpdatePosition(string id, int x, int y)
    {
        var btn = Find(id);
        if (btn != null)
        {
            btn.PositionX = x;
            btn.PositionY = y;
        }
    }

    public void LoadFrom(List<VirtualButton> buttons)
    {
        _buttons.Clear();
        _buttons.AddRange(buttons);
        _nextIndex = _buttons.Count + 1;
        ButtonsChanged?.Invoke();
    }

    public bool IsIdBound(string id) => _buttons.Exists(b => b.BindActionId == id && b.BindActionId != null);
}
