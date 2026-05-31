using System.IO;
using System.Windows;
using System.Windows.Threading;
using PrivacyMasker.Services;

namespace PrivacyMasker;

public partial class App : System.Windows.Application
{
    public App()
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            NativeMethods.TryEnableDpiAwareness();
            base.OnStartup(e);

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            ShowStartupError(ex);
            Shutdown(1);
        }
    }

    private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ShowStartupError(e.Exception);
        e.Handled = true;
        Current.Shutdown(1);
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            WriteErrorLog(ex);
        }
    }

    private static void ShowStartupError(Exception ex)
    {
        WriteErrorLog(ex);
        System.Windows.MessageBox.Show(
            ex.ToString(),
            "Privacy Masker 启动失败",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private static void WriteErrorLog(Exception ex)
    {
        try
        {
            var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(logDirectory);
            var logPath = Path.Combine(logDirectory, $"startup-{DateTime.Now:yyyyMMdd-HHmmss}.log");
            File.WriteAllText(logPath, ex.ToString());
        }
        catch
        {
            // If logging fails, the message box still gives the user the real exception.
        }
    }
}
