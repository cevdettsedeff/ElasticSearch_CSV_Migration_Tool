using ElasticSearchPostgreSQLMigrationTool.Services;
using ElasticsearchPostgreSQLMigrationTool.Services;
using ElasticSearchPostgreSQLMigrationTool.Interfaces;
using ElasticSearchPostgreSQLMigrationTool.Models;
using ElasticSearchPostgreSQLMigrationTool.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ElasticSearchPostgreSQLMigrationTool.Infrastructure
{
    /// <summary>
    /// Dependency Injection setup için basit service provider wrapper
    /// </summary>
    public static class ServiceProvider
    {
        /// <summary>
        /// Tüm servisleri register eder ve ServiceProvider döndürür
        /// </summary>
        /// <param name="settings">Migration ayarları</param>
        /// <param name="logger">Logger instance (opsiyonel, otomatik oluşturulur)</param>
        /// <returns>Configured ServiceProvider</returns>
        public static IServiceProvider BuildServiceProvider(MigrationSettings settings, ILogger? logger = null)
        {
            var services = new ServiceCollection();

            // Configuration ve Core Services
            services.AddSingleton(settings);

            // Logger - eğer verilmediyse ConsoleLogger kullan
            if (logger != null)
            {
                services.AddSingleton(logger);
            }
            else
            {
                services.AddSingleton<ILogger>(new ConsoleLogger(settings.LogLevel));
            }

            // Validators
            services.AddSingleton<IValidator<AccessLog>, AccessLogValidator>();
            services.AddSingleton<IValidator<MigrationSettings>, MigrationSettingsValidator>();

            // Application Services
            services.AddTransient<ICSVService, CSVService>();
            services.AddTransient<IPostgreSQLService, PostgreSQLService>();
            services.AddTransient<IValidationService, ValidationService>();
            services.AddTransient<IMigrationService, MigrationService>();
            services.AddTransient<IConfigurationService, ConfigurationService>();

            // ElasticSearch service (eğer yoksa ekleyin)
            services.AddTransient<IElasticSearchService, ElasticSearchService>();

            return new DefaultServiceProviderFactory().CreateServiceProvider(services);
        }

        /// <summary>
        /// WPF için özel ServiceProvider (WPFLogger ile)
        /// </summary>
        /// <param name="settings">Migration ayarları</param>
        /// <param name="wpfLogger">WPF Logger instance</param>
        /// <returns>WPF için configured ServiceProvider</returns>
        public static IServiceProvider BuildWPFServiceProvider(MigrationSettings settings, ILogger wpfLogger)
        {
            return BuildServiceProvider(settings, wpfLogger);
        }

        /// <summary>
        /// Console için ServiceProvider
        /// </summary>
        /// <param name="settings">Migration ayarları</param>
        /// <returns>Console için configured ServiceProvider</returns>
        public static IServiceProvider BuildConsoleServiceProvider(MigrationSettings settings)
        {
            return BuildServiceProvider(settings);
        }
    }
}