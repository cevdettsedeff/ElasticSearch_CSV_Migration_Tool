using ElasticSearchPostgreSQLMigrationTool.Models;
using System;
using System.Threading.Tasks;

namespace ElasticSearchPostgreSQLMigrationTool.Interfaces
{
    /// <summary>
    /// Migration işlemleri için hibrit servis interface'i
    /// Hem ElasticSearch hem de CSV kaynaklarını destekler
    /// </summary>
    public interface IMigrationService
    {
        /// <summary>
        /// ElasticSearch'den PostgreSQL'e migration işlemini başlatır ve tamamlar
        /// </summary>
        /// <returns>Migration sonuç bilgileri</returns>
        Task<MigrationResult> MigrateFromElasticSearchAsync();

        /// <summary>
        /// CSV dosyasından PostgreSQL'e migration işlemini başlatır ve tamamlar
        /// </summary>
        /// <param name="csvFilePath">CSV dosya yolu</param>
        /// <returns>Migration sonuç bilgileri</returns>
        Task<MigrationResult> MigrateFromCSVAsync(string csvFilePath);

        /// <summary>
        /// Dry run modunda ElasticSearch migration analizi yapar
        /// </summary>
        /// <returns>Analiz sonuçları</returns>
        Task<MigrationResult> AnalyzeElasticSearchAsync();

        /// <summary>
        /// Dry run modunda CSV migration analizi yapar
        /// </summary>
        /// <param name="csvFilePath">CSV dosya yolu</param>
        /// <returns>Analiz sonuçları</returns>
        Task<MigrationResult> AnalyzeCSVAsync(string csvFilePath);

        /// <summary>
        /// Migration işleminin ilerlemesini takip etmek için event
        /// </summary>
        event EventHandler<MigrationProgressEventArgs>? ProgressChanged;
    }
}