using ElasticSearchPostgreSQLMigrationTool.ViewModels;
using Microsoft.Win32;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ElasticSearchPostgreSQLMigrationTool
{ 
/// <summary>
    /// MainWindow.xaml için etkileşim mantığı
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // Window events
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitializeAsync();

            // LogOutput değişikliklerini dinle ve otomatik scroll yap
            _viewModel.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(MainViewModel.LogOutput))
                {
                    // UI thread'de scroll işlemini yap
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        LogScrollViewer.ScrollToEnd();
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
            };
        }

        private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_viewModel.IsMigrationRunning)
            {
                var result = MessageBox.Show(
                    "Migration is currently running. Are you sure you want to close the application?",
                    "Confirm Close",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }

                await _viewModel.CancelMigrationAsync();
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select CSV File",
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                FilterIndex = 1,
                Multiselect = false,
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _viewModel.SelectedFilePath = openFileDialog.FileName;
                _ = Task.Run(() => _viewModel.AnalyzeSelectedFileAsync());
            }
        }

        private async void ValidateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _viewModel.ValidateCSVAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Validation failed: {ex.Message}",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void StartMigrationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Confirmation dialog
                var message = _viewModel.DryRun
                    ? "Start CSV analysis (Dry Run mode)?"
                    : $"Start migration process?\n\nThis will import data from:\n{_viewModel.SelectedFilePath}\n\nTo PostgreSQL database.";

                var result = MessageBox.Show(
                    message,
                    "Confirm Migration",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await _viewModel.StartMigrationAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Migration failed: {ex.Message}",
                    "Migration Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to cancel the migration?",
                "Confirm Cancel",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await _viewModel.CancelMigrationAsync();
            }
        }
    }
}