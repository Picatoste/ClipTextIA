using System;
using System.Windows.Forms;

public interface IHotkeyService
{
  bool HotkeyActive { get; }
  void RegisterHotkey(Action onHotkeyPressed, Func<KeyEventArgs, bool> onHotkeysRegisteredCheck);
  void UnregisterHotkey();
  void ToggleHotkey(Action onHotkeyPressed, Action<string> onStatusChanged);
  void AutoRegisterHotkeyAfter(TimeSpan afterTimeSpan, Action onHotkeyPressed, Func<KeyEventArgs, bool> onHotkeysRegisteredCheck, Action<string> onStatusChanged);
}