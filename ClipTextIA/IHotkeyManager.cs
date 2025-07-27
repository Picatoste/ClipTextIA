using System;
using Windows.System;

namespace ClipTextIA
{
    public interface IHotkeyManager
    {
        void RegisterHotkey(VirtualKey key, HotkeyModifiers modifiers, Action action);
    }
}