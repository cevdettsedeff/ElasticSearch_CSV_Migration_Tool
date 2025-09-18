using ElasticSearchPostgreSQLMigrationTool.Enums;

namespace ElasticSearchPostgreSQLMigrationTool.Models
{
    /// <summary>
    /// Migration işlemi için gerekli tüm yapılandırma ayarlarını içerir
    /// </summary>
    public class MigrationSettings
    {
        #region Database Settings

        /// <summary>
        /// PostgreSQL veritabanı bağlantı stringi
        /// </summary>
        public string? PostgreConnectionString { get; set; }

        #endregion

        #region ElasticSearch Settings

        /// <summary>
        /// ElasticSearch node URL'leri
        /// </summary>
        public string[]? ElasticSearchNodes { get; set; }

        /// <summary>
        /// ElasticSearch index adı
        /// </summary>
        public string? IndexName { get; set; }

        /// <summary>
        /// ElasticSearch scroll timeout süresi (varsayılan: "10m")
        /// </summary>
        public string ScrollTimeout { get; set; } = "10m";

        /// <summary>
        /// ElasticSearch request timeout süresi (dakika cinsinden, varsayılan: 5)
        /// </summary>
        public int RequestTimeoutMinutes { get; set; } = 5;

        #endregion

        #region Processing Settings

        /// <summary>
        /// Her batch'te işlenecek kayıt sayısı (varsayılan: 1000)
        /// </summary>
        public int BatchSize { get; set; } = 1000;

        /// <summary>
        /// Migration işleminin paralel olarak çalıştırılıp çalıştırılmayacağı
        /// </summary>
        public bool EnableParallelProcessing { get; set; } = false;

        /// <summary>
        /// Paralel işlemede kullanılacak maksimum thread sayısı
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// Hata durumunda migration işleminin durdurulup durdurulmayacağı
        /// </summary>
        public bool StopOnError { get; set; } = true;

        /// <summary>
        /// Duplicate kayıtların ignore edilip edilmeyeceği
        /// </summary>
        public bool IgnoreDuplicates { get; set; } = true;

        /// <summary>
        /// Dry run mode - sadece kayıt sayısını gösterir, aktarım yapmaz
        /// </summary>
        public bool DryRun { get; set; } = false;

        /// <summary>
        /// Migration başlamadan önce target tablosunun temizlenip temizlenmeyeceği
        /// </summary>
        public bool TruncateBeforeMigration { get; set; } = false;

        /// <summary>
        /// Logging seviyesi
        /// </summary>
        public LogLevel LogLevel { get; set; } = LogLevel.Info;

        #endregion

        #region Validation

        /// <summary>
        /// Settings'lerin geçerli olup olmadığını kontrol eder
        /// </summary>
        public bool IsValid()
        {
            // PostgreSQL connection string kontrolü
            if (string.IsNullOrEmpty(PostgreConnectionString))
                return false;

            // Batch size kontrolü
            if (BatchSize <= 0 || BatchSize > 10000)
                return false;

            // Paralel işlem kontrolü
            if (EnableParallelProcessing && MaxDegreeOfParallelism <= 0)
                return false;

            return true;
        }

        /// <summary>
        /// ElasticSearch ayarlarının geçerli olup olmadığını kontrol eder
        /// </summary>
        public bool IsElasticSearchConfigured()
        {
            return ElasticSearchNodes != null &&
                   ElasticSearchNodes.Length > 0 &&
                   !string.IsNullOrEmpty(IndexName);
        }

        #endregion
    }
}