using Autofac;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
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

            builder.RegisterType<GeminiService>().As<IGeminiService>().SingleInstance();
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
