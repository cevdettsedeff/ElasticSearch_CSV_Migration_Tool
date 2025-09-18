using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticSearchPostgreSQLMigrationTool.Models
{
    /// <summary>
    /// Migration progress event arguments
    /// </summary>
    public class MigrationProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Mevcut batch numarası
        /// </summary>
        public int CurrentBatch { get; set; }

        /// <summary>
        /// Toplam batch sayısı
        /// </summary>
        public int TotalBatches { get; set; }

        /// <summary>
        /// Bu batch'te işlenen kayıt sayısı
        /// </summary>
        public int RecordsInBatch { get; set; }

        /// <summary>
        /// Toplam işlenen kayıt sayısı
        /// </summary>
        public int TotalRecordsProcessed { get; set; }

        /// <summary>
        /// Progress mesajı
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// İşlem başladığından bu yana geçen süre
        /// </summary>
        public TimeSpan Elapsed { get; set; }

        /// <summary>
        /// Tahmini kalan süre
        /// </summary>
        public TimeSpan? EstimatedTimeRemaining { get; set; }

        /// <summary>
        /// Progress yüzdesi (0-100)
        /// </summary>
        public double ProgressPercentage => TotalBatches > 0
            ? (double)CurrentBatch / TotalBatches * 100
            : 0;
    }
}
