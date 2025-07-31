using InputSimulatorStandard;
using InputSimulatorStandard.Native;
using Markdig;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.Storage;

namespace ClipTextIA
{
  public sealed partial class MainWindow : Window
  {
    private readonly IClipboardHelper _clipboardHelper;
    private readonly IPromptService _geminiService;
    private readonly IToastService _toastService;
    private readonly IActiveWindowInfoService _activeWindowInfoService;
    private readonly IHotkeyService _hotkeyService;

    private const string ErrorTag = "[$ERROR$]";
    public MainWindow(
        IClipboardHelper clipboardHelper,
        IPromptService geminiService,
        IToastService toastService,
        IActiveWindowInfoService activeWindowInfoService,
        IHotkeyService hotkeyService)
    {
      this.InitializeComponent();


      var settings = ApplicationData.Current.LocalSettings;
      if (settings.Values.TryGetValue("ApiKey", out var apiKey))
        ApiKeyBox.Text = apiKey as string;

      if (settings.Values.TryGetValue("ApiUrl", out var apiUrl))
        ApiUrlBox.Text = apiUrl as string;

      // También puedes guardar automáticamente al cambiar texto (opcional)
      ApiKeyBox.TextChanged += (s, e) =>
          settings.Values["ApiKey"] = ApiKeyBox.Text;

      ApiUrlBox.TextChanged += (s, e) =>
          settings.Values["ApiUrl"] = ApiUrlBox.Text;

      var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
      var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
      var appWindow = AppWindow.GetFromWindowId(windowId);

      // Tamaño inicial
      appWindow.Resize(new SizeInt32(600, 800));



      _clipboardHelper = clipboardHelper;
      _geminiService = geminiService;
      _toastService = toastService;
      _activeWindowInfoService = activeWindowInfoService;
      _hotkeyService = hotkeyService;
      this.Closed += MainWindow_Closed;

      _hotkeyService.AutoRegisterHotkeyAfter(TimeSpan.FromSeconds(5), () => _ = OnActionHotkeyAsync(), (e) => e.Control && e.KeyCode == Keys.M, HotkeyStatusChanged);

    }

    private void GlobalHook_KeyDownHandler(object sender, KeyEventArgs e)
    {
      _ = OnActionHotkeyAsync();
      e.Handled = true;
    }

    public async Task OnActionHotkeyAsync()
    {
      try
      {
        string app = _activeWindowInfoService.GetActiveWindowProcessName();
        string windowTitle = _activeWindowInfoService.GetActiveWindowTitle();

        string clipboardText = await _clipboardHelper.GetTextAsync();

        if (!string.IsNullOrWhiteSpace(clipboardText))
        {
          string userPrompt = PromptBox.Text;
          string contextualPrompt = $@"
            Eres un experto en redacción de textos. Tu tarea es mejorar el texto proporcionado, sin modificar mucho la estructura y según estas directrices:

            - El texto mejorado será insertado en un campo de entrada de una aplicación.
            - Responde solo con el texto mejorado.
            - Si la aplicación lo permite, el texto puede estar en HTML, Markdown o texto plano. Debes deducir el formato correcto según el contexto.
            - Si la aplicación destino soporta HTML, prioriza usar etiquetas HTML para el formato (ej. <b>, <i>, <p>, <br>). En caso de que la aplicación no soporte HTML o tengas dudas prioriza entonces texto plano.
            - La aplicación destino es: ""{app}"".
            - El título de la ventana de la aplicación es: ""{windowTitle}"".
            ";
          if (!string.IsNullOrWhiteSpace(userPrompt))
          {
            contextualPrompt += $"\n- {userPrompt}";
          }
          _toastService.ShowToast("\uD83D\uDE80 Iniciando mejora...");

          string improved = await _geminiService.ImproveTextAsync(ApiKeyBox.Text, ApiUrlBox.Text, contextualPrompt, clipboardText);

          if (improved.StartsWith(ErrorTag))
          {
            _toastService.ShowToast($"\u26A0 Error al mejorar el texto: {improved.Replace(ErrorTag, "").Trim()}");
            return;
          }

          await LoadMarkdownAndCopyToClipboardAsync(improved);

          var sim = new InputSimulator();
          sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);

          _toastService.ShowToast(
              "\u2705 Texto mejorado y pegado \uD83D\uDCCB\n" +
              "\uD83D\uDDA5\uFE0F App: " + app + "\n" +
              "Ventana: " + windowTitle
          );
        }
        else
        {
          _toastService.ShowToast("\u26A0 No hay texto en el portapapeles");
        }
      }
      catch (Exception ex)
      {
        _toastService.ShowToast($"\uD83D\uDD25 Error inesperado: {ex.Message}");
      }
    }

    public void ActivateHotkey_Click(object sender, RoutedEventArgs e)
    {
      _hotkeyService.ToggleHotkey(
          () => _ = OnActionHotkeyAsync(),
          HotkeyStatusChanged);
    }

    public void HotkeyStatusChanged(string status)
    {
      if (status == "activado")
      {
        _toastService.ShowToast("\u2705 Atajo activado (Ctrl+M)");
        ActivateHotkeyButton.Content = "Desactivar Hotkey";
      }
      else
      {
        _toastService.ShowToast("\u26D4 Atajo desactivado");
        ActivateHotkeyButton.Content = "Activar Hotkey";
      }
    }

    private async Task LoadMarkdownAndCopyToClipboardAsync(string markdown)
    {
      string plainText = await Task.Run(() => Markdown.ToPlainText(markdown));
      string html = await Task.Run(() => Markdown.ToHtml(markdown));

      await EnqueueAsync(async () =>
      {
        await MyWebView.EnsureCoreWebView2Async(null);
        MyWebView.NavigateToString($"<html><body>{html}</body></html>");

        CopyToClipboard(plainText, html);
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

    private void CopyToClipboard(string plainText, string html)
    {
      var dataPackage = new DataPackage();
      var htmlFormat = HtmlFormatHelper.CreateHtmlFormat(html);
      dataPackage.SetHtmlFormat(htmlFormat);
      dataPackage.SetText(plainText);
      Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
    }

    private bool IsHtml(string text)
    {
      if (string.IsNullOrWhiteSpace(text))
        return false;

      // Busca cualquier etiqueta HTML simple: <cualquier_cosa>
      return Regex.IsMatch(text, @"<[^>]+>");
    }

    //private string HtmlClipboardFormat(string htmlFragment)
    //{
    //  const string Header =
    //      "Version:0.9\r\n" +
    //      "StartHTML:{0:00000000}\r\n" +
    //      "EndHTML:{1:00000000}\r\n" +
    //      "StartFragment:{2:00000000}\r\n" +
    //      "EndFragment:{3:00000000}\r\n";

    //  const string StartFragment = "<!--StartFragment-->";
    //  const string EndFragment = "<!--EndFragment-->";

    //  string pre = "<html><body>";
    //  string post = "</body></html>";

    //  string html = pre + StartFragment + htmlFragment + EndFragment + post;

    //  int startHTML = Header.Length;
    //  int startFragment = startHTML + pre.Length + StartFragment.Length;
    //  int endFragment = startFragment + htmlFragment.Length;
    //  int endHTML = startHTML + html.Length;

    //  string header = string.Format(Header, startHTML, endHTML, startFragment, endFragment);

    //  return header + html;
    //}

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
      _hotkeyService?.UnregisterHotkey();
    }
  }
}
