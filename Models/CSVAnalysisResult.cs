using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticSearchPostgreSQLMigrationTool.Models
{
    /// <summary>
    /// CSV analiz sonucu modeli
    /// </summary>
    public class CSVAnalysisResult
    {
        /// <summary>
        /// Analiz edilen CSV dosyasının tam yolu
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Analizin yapıldığı zaman
        /// </summary>
        public DateTime AnalysisTime { get; set; }

        /// <summary>
        /// Analiz işleminin süresi
        /// </summary>
        public TimeSpan AnalysisDuration { get; set; }

        /// <summary>
        /// Analizin başarılı olup olmadığı
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Analiz sırasında oluşan hata mesajı (varsa)
        /// </summary>
        public string? AnalysisError { get; set; }

        /// <summary>
        /// Dosya boyutu (byte cinsinden)
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Dosya boyutu (MB cinsinden)
        /// </summary>
        public double FileSizeMB { get; set; }

        /// <summary>
        /// Dosyadaki toplam kayıt sayısı
        /// </summary>
        public long TotalRecords { get; set; }

        /// <summary>
        /// CSV dosyasındaki header'lar (kolon isimleri)
        /// </summary>
        public string[]? Headers { get; set; }

        /// <summary>
        /// Toplam kolon sayısı
        /// </summary>
        public int ColumnCount { get; set; }

        /// <summary>
        /// Sistem tarafından tanınan kolonlar
        /// </summary>
        public string[]? RecognizedColumns { get; set; }

        /// <summary>
        /// Sistem tarafından tanınmayan kolonlar
        /// </summary>
        public string[]? UnrecognizedColumns { get; set; }

        /// <summary>
        /// Analiz için alınan sample boyutu
        /// </summary>
        public int SampleSize { get; set; }

        /// <summary>
        /// Sample içindeki geçerli kayıt sayısı
        /// </summary>
        public int ValidSampleCount { get; set; }

        /// <summary>
        /// Sample içindeki geçersiz kayıt sayısı
        /// </summary>
        public int InvalidSampleCount { get; set; }

        /// <summary>
        /// Tahmini geçerlilik yüzdesi
        /// </summary>
        public double EstimatedValidPercentage { get; set; }

        /// <summary>
        /// Tahmini geçerli kayıt sayısı (tüm dosya için)
        /// </summary>
        public long EstimatedValidRecords { get; set; }

        /// <summary>
        /// Sample analizi sırasında oluşan hatalar
        /// </summary>
        public string[]? SampleErrors { get; set; }

        /// <summary>
        /// Analiz sonuçlarının özetini döndürür
        /// </summary>
        /// <returns>Formatlanmış özet string</returns>
        public string GetSummary()
        {
            return $@"
CSV Analysis Summary:
====================
File: {FilePath}
Size: {FileSizeMB:F1} MB ({TotalRecords:N0} records)
Columns: {ColumnCount} total, {RecognizedColumns?.Length ?? 0} recognized
Sample: {ValidSampleCount}/{SampleSize} valid ({EstimatedValidPercentage:F1}%)
Estimated Valid Records: {EstimatedValidRecords:N0}
Analysis Time: {AnalysisDuration.TotalMilliseconds:F0}ms
Status: {(IsValid ? "READY" : "ISSUES DETECTED")}";
        }
    }
}
