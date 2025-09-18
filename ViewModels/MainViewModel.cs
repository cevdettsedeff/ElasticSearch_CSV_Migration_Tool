using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElasticSearchPostgreSQLMigrationTool.Enums;
using ElasticSearchPostgreSQLMigrationTool.Infrastructure;
using ElasticSearchPostgreSQLMigrationTool.Interfaces;
using ElasticSearchPostgreSQLMigrationTool.Models;
using ElasticSearchPostgreSQLMigrationTool.Services;
using ElasticSearchPostgreSQLMigrationTool.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace ElasticSearchPostgreSQLMigrationTool.ViewModels
{
    /// <summary>
    /// Ana window için ViewModel
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        private IServiceProvider? _serviceProvider;
        private readonly MigrationSettings _settings;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private IMigrationService? _migrationService;
        private ICSVService? _csvService;
        private Task<MigrationResult>? _currentMigrationTask;

        [ObservableProperty]
        private string _selectedFilePath = string.Empty;

        [ObservableProperty]
        private string _fileInfo = string.Empty;

        [ObservableProperty]
        private Visibility _fileInfoVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private int _batchSize = 1000;

        [ObservableProperty]
        private bool _dryRun = false;

        [ObservableProperty]
        private bool _truncateBeforeMigration = false;

        [ObservableProperty]
        private bool _enableParallelProcessing = false;

        [ObservableProperty]
        private bool _ignoreDuplicates = true;

        [ObservableProperty]
        private string _connectionString = "Host=localhost;Database=migration_db;Username=postgres;Password=yourpassword;";

        [ObservableProperty]
        private string _progressMessage = "Ready to start migration...";

        [ObservableProperty]
        private double _progressPercentage = 0;

        [ObservableProperty]
        private string _progressPercentageText = "0%";

        [ObservableProperty]
        private string _processedRecordsText = "0 / 0 records";

        [ObservableProperty]
        private string _speedText = "0 records/sec";

        [ObservableProperty]
        private string _etaText = "ETA: --:--:--";

        [ObservableProperty]
        private string _logOutput = string.Empty;

        [ObservableProperty]
        private bool _canValidate = false;

        [ObservableProperty]
        private bool _canStartMigration = false;

        [ObservableProperty]
        private bool _canCancel = false;

        [ObservableProperty]
        private bool _isMigrationRunning = false;

        public MainViewModel()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _settings = LoadSettings();

            // Update settings from properties
            UpdateSettingsFromProperties();

            SetupDependencyInjection();
        }

        /// <summary>
        /// ViewModel'i başlatır
        /// </summary>
        public async Task InitializeAsync()
        {
            AddLog("🚀 CSV to PostgreSQL Migration Tool v2.0.0");
            AddLog("📝 Application initialized successfully");
            AddLog("📁 Select a CSV file to begin");

            // Test database connection
            await TestDatabaseConnectionAsync();
        }

        /// <summary>
        /// Seçili dosyayı analiz eder
        /// </summary>
        public async Task AnalyzeSelectedFileAsync()
        {
            if (string.IsNullOrEmpty(SelectedFilePath) || !File.Exists(SelectedFilePath))
                return;

            try
            {
                AddLog($"🔍 Analyzing file: {Path.GetFileName(SelectedFilePath)}");

                _csvService ??= _serviceProvider?.GetRequiredService<ICSVService>();
                if (_csvService == null)
                {
                    AddLog("❌ CSV Service not available");
                    return;
                }

                var analysis = await _csvService.AnalyzeCSVAsync(SelectedFilePath);

                if (analysis.IsValid)
                {
                    FileInfo = $"📊 {analysis.TotalRecords:N0} records, {analysis.FileSizeMB:F1} MB, {analysis.EstimatedValidPercentage:F1}% valid";
                    FileInfoVisibility = Visibility.Visible;
                    CanValidate = true;
                    CanStartMigration = true;

                    AddLog($"✅ Analysis complete: {analysis.EstimatedValidRecords:N0} valid records found");
                    AddLog($"📋 Recognized columns: {analysis.RecognizedColumns?.Length}/{analysis.ColumnCount}");

                    if (analysis.UnrecognizedColumns?.Any() == true)
                    {
                        AddLog($"⚠️ Unrecognized columns: {string.Join(", ", analysis.UnrecognizedColumns.Take(5))}");
                    }
                }
                else
                {
                    FileInfo = "❌ Invalid CSV file";
                    FileInfoVisibility = Visibility.Visible;
                    CanValidate = false;
                    CanStartMigration = false;
                    AddLog($"❌ Analysis failed: {analysis.AnalysisError}");
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ Analysis error: {ex.Message}");
                FileInfo = "❌ Analysis failed";
                FileInfoVisibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// CSV dosyasını validate eder
        /// </summary>
        public async Task ValidateCSVAsync()
        {
            if (string.IsNullOrEmpty(SelectedFilePath))
                return;

            try
            {
                AddLog("🔍 Validating CSV file...");

                _csvService ??= _serviceProvider?.GetRequiredService<ICSVService>();
                if (_csvService == null)
                {
                    throw new InvalidOperationException("CSV Service not available");
                }

                var isValid = await _csvService.ValidateFileAsync(SelectedFilePath);
                if (isValid)
                {
                    var analysis = await _csvService.AnalyzeCSVAsync(SelectedFilePath);
                    AddLog("✅ CSV validation successful");
                    AddLog(analysis.GetSummary());
                }
                else
                {
                    AddLog("❌ CSV validation failed");
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ Validation error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Migration'ı başlatır
        /// </summary>
        public async Task StartMigrationAsync()
        {
            if (string.IsNullOrEmpty(SelectedFilePath) || IsMigrationRunning)
                return;

            try
            {
                IsMigrationRunning = true;
                CanStartMigration = false;
                CanCancel = true;
                CanValidate = false;

                // Update settings from UI
                UpdateSettingsFromProperties();
                SetupDependencyInjection();

                AddLog(DryRun
                    ? "🧪 Starting CSV analysis (Dry Run mode)..."
                    : "🚀 Starting migration process...");

                _migrationService = _serviceProvider?.GetRequiredService<IMigrationService>();
                if (_migrationService == null)
                {
                    throw new InvalidOperationException("Migration Service not available");
                }

                // Subscribe to progress events
                _migrationService.ProgressChanged += OnMigrationProgressChanged;

                var stopwatch = Stopwatch.StartNew();

                // Run migration on background thread
                _currentMigrationTask = Task.Run(async () =>
                {
                    if (DryRun)
                    {
                        return await _migrationService.AnalyzeCSVAsync(SelectedFilePath);
                    }
                    else
                    {
                        return await _migrationService.MigrateFromCSVAsync(SelectedFilePath);
                    }
                }, _cancellationTokenSource.Token);

                var migrationResult = await _currentMigrationTask;
                stopwatch.Stop();

                // Show completion dialog
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ShowCompletionDialog(migrationResult);
                });

                AddLog($"⏱️ Total time: {stopwatch.Elapsed:hh\\:mm\\:ss}");
            }
            catch (OperationCanceledException)
            {
                AddLog("⚠️ Migration was cancelled by user");
            }
            catch (Exception ex)
            {
                AddLog($"❌ Migration error: {ex.Message}");
                throw;
            }
            finally
            {
                if (_migrationService != null)
                {
                    _migrationService.ProgressChanged -= OnMigrationProgressChanged;
                }

                IsMigrationRunning = false;
                CanStartMigration = !string.IsNullOrEmpty(SelectedFilePath);
                CanCancel = false;
                CanValidate = !string.IsNullOrEmpty(SelectedFilePath);

                ProgressPercentage = 0;
                ProgressMessage = "Migration completed";
                ProgressPercentageText = "100%";
            }
        }

        /// <summary>
        /// Migration'ı iptal eder
        /// </summary>
        public async Task CancelMigrationAsync()
        {
            if (!IsMigrationRunning)
                return;

            try
            {
                _cancellationTokenSource.Cancel();
                AddLog("⚠️ Cancellation requested...");

                if (_currentMigrationTask != null)
                {
                    await _currentMigrationTask;
                }
            }
            catch (OperationCanceledException)
            {
                AddLog("✅ Migration cancelled successfully");
            }
        }

        /// <summary>
        /// Migration progress event handler
        /// </summary>
        private void OnMigrationProgressChanged(object? sender, MigrationProgressEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ProgressMessage = e.Message;
                ProgressPercentage = e.ProgressPercentage;
                ProgressPercentageText = $"{e.ProgressPercentage:F1}%";
                ProcessedRecordsText = $"{e.TotalRecordsProcessed:N0} / {e.TotalRecordsProcessed + (e.TotalBatches - e.CurrentBatch) * e.RecordsInBatch:N0} records";

                var speed = e.Elapsed.TotalSeconds > 0 ? e.TotalRecordsProcessed / e.Elapsed.TotalSeconds : 0;
                SpeedText = $"{speed:F0} records/sec";

                EtaText = e.EstimatedTimeRemaining.HasValue
                    ? $"ETA: {e.EstimatedTimeRemaining:hh\\:mm\\:ss}"
                    : "ETA: Calculating...";

                AddLog($"📊 Batch {e.CurrentBatch}/{e.TotalBatches} - {e.RecordsInBatch} records processed");
            });
        }

        /// <summary>
        /// Completion dialog gösterir
        /// </summary>
        private void ShowCompletionDialog(MigrationResult result)
        {
            var message = result.Success
                ? $"🎉 Migration completed successfully!\n\n" +
                  $"📊 Records processed: {result.TotalRecordsProcessed:N0}\n" +
                  $"✅ Records inserted: {result.RecordsInserted:N0}\n" +
                  $"⏱️ Duration: {result.Duration:hh\\:mm\\:ss}\n" +
                  $"🚀 Speed: {result.RecordsPerSecond:F0} records/second"
                : $"❌ Migration failed!\n\n" +
                  $"Error: {result.ErrorMessage}\n" +
                  $"📊 Records processed: {result.TotalRecordsProcessed:N0}";

            var title = result.Success ? "Migration Completed" : "Migration Failed";
            var icon = result.Success ? MessageBoxImage.Information : MessageBoxImage.Error;

            MessageBox.Show(message, title, MessageBoxButton.OK, icon);
        }

        /// <summary>
        /// Veritabanı bağlantısını test eder
        /// </summary>
        private async Task TestDatabaseConnectionAsync()
        {
            try
            {
                AddLog("🔗 Testing database connection...");

                var postgresService = _serviceProvider?.GetRequiredService<IPostgreSQLService>();
                if (postgresService == null)
                {
                    AddLog("❌ PostgreSQL Service not available");
                    return;
                }

                var isConnected = await postgresService.TestConnectionAsync();

                if (isConnected)
                {
                    AddLog("✅ Database connection successful");
                }
                else
                {
                    AddLog("❌ Database connection failed - Please check your connection settings");
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ Database connection error: {ex.Message}");
            }
        }

        /// <summary>
        /// Settings'i property'lerden günceller
        /// </summary>
        private void UpdateSettingsFromProperties()
        {
            _settings.BatchSize = BatchSize;
            _settings.DryRun = DryRun;
            _settings.TruncateBeforeMigration = TruncateBeforeMigration;
            _settings.EnableParallelProcessing = EnableParallelProcessing;
            _settings.IgnoreDuplicates = IgnoreDuplicates;
            _settings.PostgreConnectionString = ConnectionString;
        }

        /// <summary>
        /// Ayarları yükler
        /// </summary>
        private MigrationSettings LoadSettings()
        {
            try
            {
                var configService = new ConfigurationService(new MigrationSettingsValidator());
                var settings = configService.GetSettings();

                // UI'ya yansıt
                BatchSize = settings.BatchSize;
                DryRun = settings.DryRun;
                TruncateBeforeMigration = settings.TruncateBeforeMigration;
                EnableParallelProcessing = settings.EnableParallelProcessing;
                IgnoreDuplicates = settings.IgnoreDuplicates;
                ConnectionString = settings.PostgreConnectionString ?? ConnectionString;

                return settings;
            }
            catch (Exception ex)
            {
                AddLog($"⚠️ Settings loading failed, using defaults: {ex.Message}");
                return new MigrationSettings
                {
                    BatchSize = 1000,
                    DryRun = false,
                    TruncateBeforeMigration = false,
                    EnableParallelProcessing = false,
                    IgnoreDuplicates = true,
                    PostgreConnectionString = ConnectionString,
                    LogLevel = LogLevel.Info
                };
            }
        }

        /// <summary>
        /// Dependency injection setup with ServiceProvider
        /// </summary>
        private void SetupDependencyInjection()
        {
            try
            {
                // WPF Logger - this reference kullanarak
                var wpfLogger = new WPFLogger(this);

                // Build ServiceProvider with WPF Logger
                _serviceProvider = Infrastructure.ServiceProvider.BuildWPFServiceProvider(_settings, wpfLogger);

                AddLog("✅ Dependency injection setup completed");
            }
            catch (Exception ex)
            {
                AddLog($"❌ ServiceProvider setup failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Log mesajı ekler
        /// </summary>
        public void AddLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var logEntry = $"[{timestamp}] {message}";

            Application.Current.Dispatcher.Invoke(() =>
            {
                LogOutput += logEntry + Environment.NewLine;

                // Scroll to end (eğer çok uzunsa son 1000 satırı tut)
                var lines = LogOutput.Split('\n');
                if (lines.Length > 1000)
                {
                    LogOutput = string.Join("\n", lines.Skip(lines.Length - 1000));
                }

                // PropertyChanged tetikle ki scroll işlemi çalışsın
                OnPropertyChanged(nameof(LogOutput));
            });
        }

        /// <summary>
        /// Resources temizle
        /// </summary>
        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            // File path değiştiğinde analiz et
            if (e.PropertyName == nameof(SelectedFilePath))
            {
                FileInfoVisibility = Visibility.Collapsed;
                CanValidate = false;
                CanStartMigration = false;
                FileInfo = string.Empty;
            }
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();

            // ServiceProvider'ı dispose et
            if (_serviceProvider is IDisposable disposableProvider)
            {
                disposableProvider.Dispose();
            }
        }
    }


}