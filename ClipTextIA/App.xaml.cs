using Autofac;
using Microsoft.UI.Xaml;
using IContainer = Autofac.IContainer;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ClipTextIA
{
  /// <summary>
  /// Provides application-specific behavior to supplement the default Application class.
  /// </summary>
  public partial class App : Application
  {
    private static IContainer Container;
    private Window? _window;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
      InitializeComponent();
    }

    public Window? Window { get => _window; set => _window = value; }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
      var builder = new ContainerBuilder();

      builder.RegisterType<GeminiService>().As<IPromptService>().SingleInstance();
      builder.RegisterType<HotkeyService>().As<IHotkeyService>().SingleInstance();
      builder.RegisterType<ClipboardHelper>().As<IClipboardHelper>().SingleInstance();
      builder.RegisterType<ToastService>().As<IToastService>().SingleInstance();
      builder.RegisterType<ActiveWindowInfoService>().As<IActiveWindowInfoService>().SingleInstance();
      builder.RegisterType<MainWindow>();

      Container = builder.Build();

      Window = Container.Resolve<MainWindow>();
      Window.Activate();
    }
  }
}
