using ElasticSearchPostgreSQLMigrationTool.Enums;
using System;
using System.Collections.Generic;

namespace ElasticSearchPostgreSQLMigrationTool.Models
{
    /// <summary>
    /// Migration işlemi sonuç bilgilerini içerir
    /// </summary>
    public class MigrationResult
    {
        public MigrationResult()
        {
            Errors = new List<string>();
            Warnings = new List<string>();
            BatchErrors = new Dictionary<int, string>();
            BatchDurations = new Dictionary<int, TimeSpan>();
            BatchesProcessed = 0;
            TotalRecordsProcessed = 0;
            RecordsInserted = 0;
            RecordsSkipped = 0;
            RecordsFailed = 0;
        }

        // Genel Bilgiler
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; private set; }
        public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : TimeSpan.Zero;

        // Kaynak Bilgileri
        public DataSourceType SourceType { get; set; } = DataSourceType.Unknown;
        public string? SourceIndex { get; set; }  // ElasticSearch için
        public string? SourceFile { get; set; }   // CSV için

        // İstatistikler
        public int TotalRecordsInSource { get; set; }
        public int TotalRecordsProcessed { get; set; }
        public int RecordsBeforeMigration { get; set; }
        public int RecordsAfterMigration { get; set; }
        public int RecordsInserted { get; set; }
        public int RecordsSkipped { get; set; }
        public int RecordsFailed { get; set; }

        // Batch Bilgileri
        public int BatchSize { get; set; }
        public int BatchesProcessed { get; set; }
        public Dictionary<int, string> BatchErrors { get; set; }
        public Dictionary<int, TimeSpan> BatchDurations { get; set; }

        // Hata ve Uyarılar
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }

        // Hesaplanmış Özellikler
        public double SuccessRate => TotalRecordsProcessed > 0 ?
            (double)RecordsInserted / TotalRecordsProcessed * 100 : 0;

        public double GetSuccessPercentage() => SuccessRate;

        public string? TargetTable { get; set; }

        public double RecordsPerSecond => Duration.TotalSeconds > 0 ?
            TotalRecordsProcessed / Duration.TotalSeconds : 0;

        public int TotalErrors => Errors.Count + BatchErrors.Count;

        public bool HasWarnings => Warnings.Count > 0;

        public bool HasErrors => TotalErrors > 0;

        // Helper Methods
        public void Complete()
        {
            EndTime = DateTime.UtcNow;
        }

        public void AddBatchError(int batchNumber, string error)
        {
            BatchErrors[batchNumber] = error;
        }

        public void AddBatchDuration(int batchNumber, TimeSpan duration)
        {
            BatchDurations[batchNumber] = duration;
        }

        public string GetSummary()
        {
            var summary = $"Migration Summary:\n";
            summary += $"Source: {SourceType}";

            if (SourceType == DataSourceType.ElasticSearch && !string.IsNullOrEmpty(SourceIndex))
                summary += $" (Index: {SourceIndex})";
            else if (SourceType == DataSourceType.CSV && !string.IsNullOrEmpty(SourceFile))
                summary += $" (File: {System.IO.Path.GetFileName(SourceFile)})";

            summary += $"\n";
            summary += $"Status: {(Success ? "SUCCESS" : "FAILED")}\n";
            summary += $"Duration: {Duration:hh\\:mm\\:ss}\n";
            summary += $"Records Processed: {TotalRecordsProcessed:N0}\n";
            summary += $"Records Inserted: {RecordsInserted:N0}\n";
            summary += $"Records Skipped: {RecordsSkipped:N0}\n";
            summary += $"Records Failed: {RecordsFailed:N0}\n";
            summary += $"Success Rate: {SuccessRate:F1}%\n";
            summary += $"Records/Second: {RecordsPerSecond:F2}\n";

            if (HasErrors)
                summary += $"Errors: {TotalErrors}\n";

            if (HasWarnings)
                summary += $"Warnings: {Warnings.Count}\n";

            return summary;
        }

        public string GetDetailedReport()
        {
            var report = GetSummary();
            report += "\n--- DETAILED INFORMATION ---\n";

            if (BatchErrors.Count > 0)
            {
                report += "\nBatch Errors:\n";
                foreach (var error in BatchErrors)
                {
                    report += $"  Batch {error.Key}: {error.Value}\n";
                }
            }

            if (Errors.Count > 0)
            {
                report += "\nGeneral Errors:\n";
                foreach (var error in Errors)
                {
                    report += $"  • {error}\n";
                }
            }

            if (Warnings.Count > 0)
            {
                report += "\nWarnings:\n";
                foreach (var warning in Warnings)
                {
                    report += $"  • {warning}\n";
                }
            }

            if (BatchDurations.Count > 0)
            {
                report += "\nBatch Performance:\n";
                foreach (var batch in BatchDurations)
                {
                    report += $"  Batch {batch.Key}: {batch.Value.TotalSeconds:F2}s\n";
                }
            }

            return report;
        }
    }
}