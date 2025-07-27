using System;
using System.Runtime.InteropServices;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace ClipTextIA
{
    public class ToastService : IToastService
    {
        private readonly string _appId;

        // Constructor registra el AppUserModelID necesario para que funcionen los toasts en WinUI3 desktop
        public ToastService()
        {
            string packageFamilyName = Windows.ApplicationModel.Package.Current.Id.FamilyName;
            _appId = $"{packageFamilyName}!App"; ; // Usa un ID más "único"

            // Registrar AppUserModelID en el proceso
            AppNotificationHelper.RegisterAppForNotification(_appId);
        }

        public void ShowToast(string message)
        {
            var toastXmlString =
                $@"<toast>
                    <visual>
                      <binding template='ToastGeneric'>
                        <text>ClipTextIA</text>
                        <text>{message}</text>
                      </binding>
                    </visual>
                  </toast>";

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(toastXmlString);

            var toast = new ToastNotification(xmlDoc);
            ToastNotificationManager.CreateToastNotifier(_appId).Show(toast);
        }
    }

    internal static class AppNotificationHelper
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int SetCurrentProcessExplicitAppUserModelID(string AppID);

        public static void RegisterAppForNotification(string appId)
        {
            SetCurrentProcessExplicitAppUserModelID(appId);
        }
    }


}