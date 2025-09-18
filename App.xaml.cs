using System.Configuration;
using System.Data;
using System.Windows;

namespace ElasticSearchPostgreSQLMigrationTool;

/// <summary>
/// App.xaml için etkileşim mantığı
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Global exception handler
        DispatcherUnhandledException += (sender, args) =>
        {
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{args.Exception.Message}",
                "Application Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            args.Handled = true;
        };

        // Set application properties
        Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Cleanup any resources here
        base.OnExit(e);
    }
}

