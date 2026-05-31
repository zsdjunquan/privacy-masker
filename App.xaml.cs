using System.Windows;
using PrivacyMasker.Services;

namespace PrivacyMasker;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        NativeMethods.TryEnableDpiAwareness();
        base.OnStartup(e);

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
