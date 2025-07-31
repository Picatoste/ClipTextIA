using Gma.System.MouseKeyHook;
using Microsoft.UI.Xaml;
using System;
using System.Windows.Forms;

namespace ClipTextIA
{
  public class HotkeyService : IHotkeyService
  {
    private IKeyboardMouseEvents _globalHook;
    private Func<KeyEventArgs, bool> _onHotkeysRegisteredCheck;
    private bool _hotkeyActive;

    public bool HotkeyActive => _hotkeyActive;

    public void RegisterHotkey(Action onHotkeyPressed, Func<KeyEventArgs, bool> onHotkeysRegisteredCheck)
    {
      if (_globalHook != null) UnregisterHotkey();

      _onHotkeysRegisteredCheck = onHotkeysRegisteredCheck;
      _globalHook = Hook.GlobalEvents();
      _globalHook.KeyDown += (sender, e) =>
      {
        if ((_onHotkeysRegisteredCheck?.Invoke(e)).GetValueOrDefault())
        {
          onHotkeyPressed?.Invoke();
          e.Handled = true;
        }
      };
      _hotkeyActive = true;
    }

    public void UnregisterHotkey()
    {
      if (_globalHook != null)
      {
        _globalHook.Dispose();
        _globalHook = null;
      }
      _hotkeyActive = false;
    }

    public void ToggleHotkey(Action onHotkeyPressed, Action<string> onStatusChanged)
    {
      if (_onHotkeysRegisteredCheck != null)
      {
        if (_hotkeyActive)
        {
          UnregisterHotkey();
          onStatusChanged?.Invoke("desactivado");
        }
        else
        {
          RegisterHotkey(onHotkeyPressed, _onHotkeysRegisteredCheck);
          onStatusChanged?.Invoke("activado");
        }
      }
    }

    public void AutoRegisterHotkeyAfter(TimeSpan afterTimeSpan, Action onHotkeyPressed, Func<KeyEventArgs, bool> onHotkeysRegisteredCheck, Action<string> onStatusChanged)
    {
      _onHotkeysRegisteredCheck = onHotkeysRegisteredCheck;
      var timer = new DispatcherTimer();
      timer.Interval = afterTimeSpan;
      timer.Tick += (s, e) =>
      {
        timer.Stop();
        if (!HotkeyActive)
        {
          ToggleHotkey(onHotkeyPressed, onStatusChanged);
        }
      };
      timer.Start();
    }
  }
}