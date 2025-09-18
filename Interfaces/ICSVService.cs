using ElasticSearchPostgreSQLMigrationTool.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ElasticSearchPostgreSQLMigrationTool.Interfaces
{
    /// <summary>
    /// Unified CSV servis interface'i - Tüm CSV işlemlerini kapsar
    /// </summary>
    public interface ICSVService
    {
        #region File Operations

        /// <summary>
        /// CSV dosyasını validate eder
        /// </summary>
        /// <param name="filePath">CSV dosya yolu</param>
        /// <returns>Geçerli ise true</returns>
        Task<bool> ValidateFileAsync(string filePath);

        /// <summary>
        /// CSV dosyasının geçerli format kontrolünü yapar
        /// </summary>
        /// <param name="csvFilePath">CSV dosya yolu</param>
        /// <returns>Geçerli ise true</returns>
        Task<bool> ValidateFormatAsync(string csvFilePath);

        /// <summary>
        /// CSV dosyasının header bilgilerini döner
        /// </summary>
        /// <param name="csvFilePath">CSV dosya yolu</param>
        /// <returns>Header listesi</returns>
        Task<IEnumerable<string>> GetHeadersAsync(string csvFilePath);

        #endregion

        #region Data Reading

        /// <summary>
        /// CSV dosyasından tüm access log kayıtlarını okur (Orijinal metod)
        /// </summary>
        /// <param name="filePath">CSV dosya yolu</param>
        /// <returns>AccessLog listesi</returns>
        Task<IEnumerable<AccessLog>> ReadAccessLogsAsync(string filePath);

        /// <summary>
        /// CSV dosyasını okur ve AccessLog listesi döner (Hibrit yaklaşım için alias)
        /// </summary>
        /// <param name="csvFilePath">CSV dosya yolu</param>
        /// <returns>AccessLog listesi</returns>
        Task<IEnumerable<AccessLog>> ReadCsvAsync(string csvFilePath);

        #endregion

        #region Record Count

        /// <summary>
        /// CSV dosyasındaki kayıt sayısını döndürür (Orijinal metod - long dönüş)
        /// </summary>
        /// <param name="filePath">CSV dosya yolu</param>
        /// <returns>Kayıt sayısı</returns>
        Task<long> GetRecordCountAsync(string filePath);

        /// <summary>
        /// CSV dosyasındaki toplam satır sayısını döner (Hibrit yaklaşım için - int dönüş)
        /// </summary>
        /// <param name="csvFilePath">CSV dosya yolu</param>
        /// <returns>Satır sayısı</returns>
        Task<int> GetRecordCountIntAsync(string csvFilePath);

        #endregion

        #region Analysis

        /// <summary>
        /// CSV dosyasını analiz eder (Detaylı analiz)
        /// </summary>
        /// <param name="filePath">CSV dosya yolu</param>
        /// <returns>Analiz sonuçları</returns>
        Task<CSVAnalysisResult> AnalyzeCSVAsync(string filePath);

        #endregion
    }
}