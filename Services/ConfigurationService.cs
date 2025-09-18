using ElasticSearchPostgreSQLMigrationTool.Enums;
using ElasticSearchPostgreSQLMigrationTool.Interfaces;
using ElasticSearchPostgreSQLMigrationTool.Models;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace ElasticSearchPostgreSQLMigrationTool.Services
{
    /// <summary>
    /// .NET Configuration sistemi ile yapılandırma servisi implementasyonu
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private MigrationSettings? _cachedSettings;
        private readonly IValidator<MigrationSettings> _validator;
        private readonly IConfiguration _configuration;

        public ConfigurationService(IValidator<MigrationSettings> validator, IConfiguration? configuration = null)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _configuration = configuration ?? BuildConfiguration();
        }

        /// <summary>
        /// Migration ayarlarını yükler (önce cache'den, sonra configuration'dan)
        /// </summary>
        public MigrationSettings GetSettings()
        {
            if (_cachedSettings == null)
            {
                _cachedSettings = LoadFromConfiguration();
                ValidateSettings();
            }
            return _cachedSettings;
        }

        /// <summary>
        /// Ayarları doğrular
        /// </summary>
        public void ValidateSettings()
        {
            var settings = _cachedSettings ?? GetSettings();

            var validationResult = _validator.Validate(settings);

            if (!validationResult.IsValid)
            {
                var errors = string.Join(Environment.NewLine, validationResult.Errors.Select(e => e.ErrorMessage));
                throw new InvalidOperationException($"Migration ayarları geçersiz:{Environment.NewLine}{errors}");
            }
        }

        /// <summary>
        /// .NET Configuration'dan ayarları yükler
        /// </summary>
        private MigrationSettings LoadFromConfiguration()
        {
            var settings = new MigrationSettings();

            // MigrationSettings section'ını bind et
            var migrationSection = _configuration.GetSection("MigrationSettings");
            migrationSection.Bind(settings);

            // ConnectionString'i ayrıca al
            settings.PostgreConnectionString = _configuration.GetConnectionString("PostgreSQL")
                ?? throw new InvalidOperationException("PostgreSQL connection string bulunamadı");

            // Environment'dan override'lar (opsiyonel)
            OverrideFromEnvironment(settings);

            return settings;
        }

        /// <summary>
        /// Environment variables'dan ayarları override eder (Docker/Production için)
        /// </summary>
        private static void OverrideFromEnvironment(MigrationSettings settings)
        {
            // Connection string override
            var envConnectionString = Environment.GetEnvironmentVariable("POSTGRESQL_CONNECTION");
            if (!string.IsNullOrEmpty(envConnectionString))
            {
                settings.PostgreConnectionString = envConnectionString;
            }

            // Batch size override
            var envBatchSize = Environment.GetEnvironmentVariable("BATCH_SIZE");
            if (int.TryParse(envBatchSize, out int batchSize))
            {
                settings.BatchSize = batchSize;
            }

            // Dry run override
            var envDryRun = Environment.GetEnvironmentVariable("DRY_RUN");
            if (bool.TryParse(envDryRun, out bool dryRun))
            {
                settings.DryRun = dryRun;
            }

            // Log level override
            var envLogLevel = Environment.GetEnvironmentVariable("LOG_LEVEL");
            if (Enum.TryParse<LogLevel>(envLogLevel, true, out LogLevel logLevel))
            {
                settings.LogLevel = logLevel;
            }

            // Truncate override
            var envTruncate = Environment.GetEnvironmentVariable("TRUNCATE_BEFORE_MIGRATION");
            if (bool.TryParse(envTruncate, out bool truncate))
            {
                settings.TruncateBeforeMigration = truncate;
            }

            // Parallel processing override
            var envParallel = Environment.GetEnvironmentVariable("ENABLE_PARALLEL_PROCESSING");
            if (bool.TryParse(envParallel, out bool parallel))
            {
                settings.EnableParallelProcessing = parallel;
            }

            // Stop on error override
            var envStopOnError = Environment.GetEnvironmentVariable("STOP_ON_ERROR");
            if (bool.TryParse(envStopOnError, out bool stopOnError))
            {
                settings.StopOnError = stopOnError;
            }
        }

        /// <summary>
        /// Configuration builder oluşturur
        /// </summary>
        private static IConfiguration BuildConfiguration()
        {
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            return builder.Build();
        }

        /// <summary>
        /// Ayarları dosyadan yükler (JSON formatında)
        /// </summary>
        public async Task<MigrationSettings> LoadFromFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Ayar dosyası bulunamadı: {filePath}");
            }

            try
            {
                // Geçici configuration builder ile dosyayı yükle
                var tempBuilder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile(filePath, optional: false);

                var tempConfig = tempBuilder.Build();
                var configService = new ConfigurationService(_validator, tempConfig);
                var settings = configService.GetSettings();

                // Cache'e kaydet
                _cachedSettings = settings;

                return settings;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ayar dosyası yüklenemedi: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Ayarları JSON dosyasına kaydeder
        /// </summary>
        public async Task SaveToFileAsync(MigrationSettings settings, string filePath)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            // Validation
            var validationResult = _validator.Validate(settings);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(Environment.NewLine, validationResult.Errors.Select(e => e.ErrorMessage));
                throw new InvalidOperationException($"Kaydedilecek ayarlar geçersiz:{Environment.NewLine}{errors}");
            }

            try
            {
                var configObject = new
                {
                    ConnectionStrings = new
                    {
                        PostgreSQL = settings.PostgreConnectionString
                    },
                    MigrationSettings = new
                    {
                        settings.BatchSize,
                        settings.EnableParallelProcessing,
                        settings.MaxDegreeOfParallelism,
                        settings.StopOnError,
                        settings.IgnoreDuplicates,
                        settings.DryRun,
                        settings.TruncateBeforeMigration,
                        LogLevel = settings.LogLevel.ToString()
                    }
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(configObject, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });

                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(filePath, jsonContent);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ayarlar dosyaya kaydedilemedi: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Örnek appsettings.json dosyası oluşturur
        /// </summary>
        public async Task CreateSampleConfigAsync(string filePath)
        {
            var sampleSettings = new MigrationSettings
            {
                PostgreConnectionString = "Host=localhost;Database=last_test_2007;Username=postgres;Password=postgres;",
                BatchSize = 1000,
                EnableParallelProcessing = false,
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                StopOnError = true,
                IgnoreDuplicates = true,
                DryRun = false,
                TruncateBeforeMigration = false,
                LogLevel = LogLevel.Info
            };

            await SaveToFileAsync(sampleSettings, filePath);
        }

        /// <summary>
        /// Mevcut configuration'ı debug için yazdırır
        /// </summary>
        public void PrintConfiguration()
        {
            Console.WriteLine("=== Configuration Debug ===");
            Console.WriteLine($"Environment: {Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}");
            Console.WriteLine($"Base Path: {Directory.GetCurrentDirectory()}");

            Console.WriteLine("\nappsettings.json locations checked:");
            Console.WriteLine($"  - {Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json")}");
            Console.WriteLine($"  - {Path.Combine(Directory.GetCurrentDirectory(), $"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json")}");

            Console.WriteLine("\nCurrent configuration values:");
            foreach (var item in _configuration.AsEnumerable())
            {
                if (!string.IsNullOrEmpty(item.Value))
                {
                    var value = item.Key.ToLower().Contains("password") ? "***" : item.Value;
                    Console.WriteLine($"  {item.Key} = {value}");
                }
            }
            Console.WriteLine("========================");
        }

        /// <summary>
        /// Configuration'ın yüklenip yüklenmediğini test eder
        /// </summary>
        public bool TestConfiguration()
        {
            try
            {
                var testSettings = GetSettings();
                return testSettings != null && !string.IsNullOrEmpty(testSettings.PostgreConnectionString);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Configuration source bilgilerini döndürür
        /// </summary>
        public ConfigurationInfo GetConfigurationInfo()
        {
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
            var basePath = Directory.GetCurrentDirectory();

            return new ConfigurationInfo
            {
                Environment = environment,
                BasePath = basePath,
                AppSettingsPath = Path.Combine(basePath, "appsettings.json"),
                EnvironmentAppSettingsPath = Path.Combine(basePath, $"appsettings.{environment}.json"),
                AppSettingsExists = File.Exists(Path.Combine(basePath, "appsettings.json")),
                EnvironmentAppSettingsExists = File.Exists(Path.Combine(basePath, $"appsettings.{environment}.json")),
                ConfigurationSources = _configuration.AsEnumerable()
                    .Where(x => !string.IsNullOrEmpty(x.Value))
                    .ToDictionary(x => x.Key, x => x.Value)
            };
        }
    }
}
