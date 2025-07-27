using Gma.System.MouseKeyHook;
using Markdig;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using InputSimulatorStandard;
using InputSimulatorStandard.Native;

namespace ClipTextIA
{
    public sealed partial class MainWindow : Window
    {
        private readonly IKeyboardMouseEvents _hotkeyService;
        private readonly IHotkeyManager _hotkeyManager;
        private readonly IClipboardHelper _clipboardHelper;
        private readonly IGeminiService _geminiService;
        private readonly IToastService _toastService;
        private readonly IActiveWindowInfoService _activeWindowInfoService;
        private IKeyboardMouseEvents _globalHook;
        private bool _hotkeyActive = false;

        public MainWindow(IHotkeyManager hotkeyManager, IClipboardHelper clipboardHelper, IGeminiService geminiService, IToastService toastService, IActiveWindowInfoService activeWindowInfoService)
        {
            this.InitializeComponent();

            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            // Tamaño inicial
            appWindow.Resize(new SizeInt32(500, 600));



            _hotkeyManager = hotkeyManager;
            _clipboardHelper = clipboardHelper;
            _geminiService = geminiService;
            _toastService = toastService;
            _activeWindowInfoService = activeWindowInfoService;
            this.Closed += MainWindow_Closed;
            _globalHook = Hook.GlobalEvents();

            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                if (!_hotkeyActive)
                {
                    RegisterOrUnRegisterHotkey();
                }
            };
            timer.Start();

        }


        public void RegisterHotkey(Action onHotkeyPressed)
        {
            _globalHook = Hook.GlobalEvents();
            _globalHook.KeyDown += (sender, e) =>
            {
                if (e.Control && e.KeyCode == Keys.M)
                {
                    onHotkeyPressed?.Invoke();
                    e.Handled = true;
                }
            };
        }

        public void UnregisterHotkey()
        {
            if (_globalHook != null)
            {
                _globalHook.KeyDown -= GlobalHook_KeyDownHandler;
                _globalHook.Dispose();
                _globalHook = null;
            }
        }

        // Para poder eliminar correctamente el handler, lo extraemos a un método:
        private void GlobalHook_KeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.M)
            {
                _ = EnqueueAsync(async () =>
                {
                    string app = _activeWindowInfoService.GetActiveWindowProcessName();
                    string windowTitle = _activeWindowInfoService.GetActiveWindowTitle();

                    string clipboardText = await _clipboardHelper.GetTextAsync();
                    if (!string.IsNullOrWhiteSpace(clipboardText))
                    {
                        string userPrompt = PromptBox.Text;
                        string contextualPrompt =
                            $"{userPrompt}\n\n" +
                            "Sin mencionar explícitamente esta información en la respuesta, ten en cuenta que el texto será pegado en la aplicación " +
                            $"\"{app}\", la cual está abierta en una ventana con el título \"{windowTitle}\". Usa este contexto para adaptar y mejorar el texto de forma adecuada.\n";

                        string improved = await _geminiService.ImproveTextAsync(contextualPrompt, clipboardText);
                        await LoadMarkdownAndCopyToClipboardAsync(improved);

                        var sim = new InputSimulator();
                        sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);

                        _toastService.ShowToast(
                            "\u2705 Texto mejorado y pegado \U0001F4CB\n" +
                            "\U0001F5A5\uFE0F App: " + app + "\n" +
                            "\U0001FA9F Ventana: " + windowTitle
                        );
                    }
                    else
                    {
                        _toastService.ShowToast("\u26A0 No hay texto en el portapapeles");
                    }
                });

                e.Handled = true;
            }
        }

        public void ActivateHotkey_Click(object sender, RoutedEventArgs e)
        {
            RegisterOrUnRegisterHotkey();
        }

        private void RegisterOrUnRegisterHotkey()
        {
            if (_hotkeyActive)
            {
                UnregisterHotkey();
                _toastService.ShowToast("\u26D4 Atajo desactivado");
                _hotkeyActive = false;
                ActivateHotkeyButton.Content = "Activar Hotkey";

            }
            else
            {
                _globalHook = Hook.GlobalEvents();
                _globalHook.KeyDown += GlobalHook_KeyDownHandler;
                _toastService.ShowToast("\u2705 Atajo activado (Ctrl+M)");
                _hotkeyActive = true;
                ActivateHotkeyButton.Content = "Desactivar Hotkey";
            }
        }


        private async Task LoadMarkdownAndCopyToClipboardAsync(string markdown)
        {
            // Convertir Markdown a HTML en hilo background
            string html = await Task.Run(() => Markdown.ToHtml(markdown));

            // Actualizar WebView2 y copiar HTML al portapapeles en hilo UI
            await EnqueueAsync(async () =>
            {
                await MyWebView.EnsureCoreWebView2Async(null);
                MyWebView.NavigateToString($"<html><body>{html}</body></html>");

                CopyHtmlToClipboard(html);
            });
        }

        public static Task EnqueueAsync(Func<Task> func)
        {
            var dispatcherQueue = Windows.System.DispatcherQueue.GetForCurrentThread();
            var tcs = new TaskCompletionSource<bool>();

            dispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    await func();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }

        private void CopyHtmlToClipboard(string html)
        {
            var dataPackage = new DataPackage();
            string htmlClipboardFormat = HtmlClipboardFormat(html);
            dataPackage.SetData(StandardDataFormats.Html, htmlClipboardFormat);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }

        private string HtmlClipboardFormat(string htmlFragment)
        {
            const string Header =
                "Version:0.9\r\n" +
                "StartHTML:{0:00000000}\r\n" +
                "EndHTML:{1:00000000}\r\n" +
                "StartFragment:{2:00000000}\r\n" +
                "EndFragment:{3:00000000}\r\n";

            const string StartFragment = "<!--StartFragment-->";
            const string EndFragment = "<!--EndFragment-->";

            string pre = "<html><body>";
            string post = "</body></html>";

            string html = pre + StartFragment + htmlFragment + EndFragment + post;

            int startHTML = Header.Length;
            int startFragment = startHTML + pre.Length + StartFragment.Length;
            int endFragment = startFragment + htmlFragment.Length;
            int endHTML = startHTML + html.Length;

            string header = string.Format(Header, startHTML, endHTML, startFragment, endFragment);

            return header + html;
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            _globalHook?.Dispose();
        }
    }
}
