using System;
using System.IO;
using System.Windows;

namespace LocalhostManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppLogger.Log("App OnStartup started");
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
                File.WriteAllText(path, args.ExceptionObject.ToString());
                AppLogger.Log($"UnhandledException: {args.ExceptionObject}");
            };
            DispatcherUnhandledException += (s, args) =>
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
                File.WriteAllText(path, args.Exception.ToString());
                AppLogger.Log($"DispatcherUnhandledException: {args.Exception}");
            };
            base.OnStartup(e);

            Services.LocalizationService.Initialize();

            var window = new MainWindow();
            this.MainWindow = window;
            window.Show();
            window.Activate();

            AppLogger.Log("App OnStartup finished");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            AppLogger.Log($"App OnExit with code {e.ApplicationExitCode}");
            base.OnExit(e);
        }
    }
}
