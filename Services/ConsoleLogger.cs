using ElasticSearchPostgreSQLMigrationTool.Models;
using ElasticSearchPostgreSQLMigrationTool.Enums;
using ElasticSearchPostgreSQLMigrationTool.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticSearchPostgreSQLMigrationTool.Services
{
    /// <summary>
    /// Console tabanlı logger implementasyonu
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        private readonly LogLevel _logLevel;
        private readonly object _lockObject = new();

        public ConsoleLogger(LogLevel logLevel = LogLevel.Info)
        {
            _logLevel = logLevel;
        }

        /// <summary>
        /// Info seviyesinde log kaydeder
        /// </summary>
        public void LogInfo(string message)
        {
            if (_logLevel >= LogLevel.Info)
            {
                WriteLog("INFO", message, ConsoleColor.White);
            }
        }

        /// <summary>
        /// Warning seviyesinde log kaydeder
        /// </summary>
        public void LogWarning(string message)
        {
            if (_logLevel >= LogLevel.Warning)
            {
                WriteLog("WARN", message, ConsoleColor.Yellow);
            }
        }

        /// <summary>
        /// Error seviyesinde log kaydeder
        /// </summary>
        public void LogError(string message)
        {
            if (_logLevel >= LogLevel.Error)
            {
                WriteLog("ERROR", message, ConsoleColor.Red);
            }
        }

        /// <summary>
        /// Exception ile birlikte error log kaydeder
        /// </summary>
        public void LogError(Exception exception, string message)
        {
            if (_logLevel >= LogLevel.Error)
            {
                var fullMessage = $"{message}: {exception.Message}";

                if (_logLevel >= LogLevel.Debug)
                {
                    fullMessage += Environment.NewLine + exception.StackTrace;
                }

                WriteLog("ERROR", fullMessage, ConsoleColor.Red);
            }
        }

        /// <summary>
        /// Debug seviyesinde log kaydeder
        /// </summary>
        public void LogDebug(string message)
        {
            if (_logLevel >= LogLevel.Debug)
            {
                WriteLog("DEBUG", message, ConsoleColor.Gray);
            }
        }

        /// <summary>
        /// Progress bilgisini loglar
        /// </summary>
        public void LogProgress(int current, int total, string message)
        {
            if (_logLevel >= LogLevel.Info)
            {
                var percentage = total > 0 ? (double)current / total * 100 : 0;
                var progressBar = CreateProgressBar(percentage);
                var progressMessage = $"{message} [{progressBar}] {current:N0}/{total:N0} ({percentage:F1}%)";

                WriteLog("PROGRESS", progressMessage, ConsoleColor.Cyan);
            }
        }

        /// <summary>
        /// Renkli log mesajını yazar
        /// </summary>
        private void WriteLog(string level, string message, ConsoleColor color)
        {
            lock (_lockObject)
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var originalColor = Console.ForegroundColor;

                try
                {
                    // Timestamp - gri renkte
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write($"[{timestamp}] ");

                    // Level - belirtilen renkte ve sabit genişlikte
                    Console.ForegroundColor = GetLevelColor(level);
                    Console.Write($"[{level,-7}] ");

                    // Message - belirtilen renkte
                    Console.ForegroundColor = color;
                    Console.WriteLine(message);
                }
                finally
                {
                    Console.ForegroundColor = originalColor;
                }
            }
        }

        /// <summary>
        /// Log level için renk döndürür
        /// </summary>
        private static ConsoleColor GetLevelColor(string level)
        {
            return level switch
            {
                "INFO" => ConsoleColor.Green,
                "WARN" => ConsoleColor.Yellow,
                "ERROR" => ConsoleColor.Red,
                "DEBUG" => ConsoleColor.DarkGray,
                "PROGRESS" => ConsoleColor.Cyan,
                _ => ConsoleColor.White
            };
        }

        /// <summary>
        /// Progress bar oluşturur
        /// </summary>
        private static string CreateProgressBar(double percentage, int width = 30)
        {
            var filled = (int)(percentage / 100 * width);
            var empty = width - filled;

            return new string('█', filled) + new string('░', empty);
        }

        /// <summary>
        /// Migration başlangıç banner'ını gösterir
        /// </summary>
        public void ShowStartupBanner()
        {
            lock (_lockObject)
            {
                var originalColor = Console.ForegroundColor;

                try
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine();
                    Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
                    Console.WriteLine("║        ElasticSearch to PostgreSQL Migration Tool           ║");
                    Console.WriteLine("║                     v1.0.0                                  ║");
                    Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
                    Console.WriteLine();
                }
                finally
                {
                    Console.ForegroundColor = originalColor;
                }
            }
        }

        /// <summary>
        /// Migration sonuç özetini gösterir
        /// </summary>
        public void ShowMigrationSummary(MigrationResult result)
        {
            lock (_lockObject)
            {
                var originalColor = Console.ForegroundColor;

                try
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
                    Console.WriteLine("║                    Migration Summary                         ║");
                    Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");

                    // Status
                    Console.ForegroundColor = result.Success ? ConsoleColor.Green : ConsoleColor.Red;
                    Console.WriteLine($"Status: {(result.Success ? "✅ SUCCESS" : "❌ FAILED")}");

                    // Duration
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"Duration: {result.Duration:hh\\:mm\\:ss}");

                    // Records
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Records Processed: {result.TotalRecordsProcessed:N0}");
                    Console.WriteLine($"Records Inserted: {result.RecordsInserted:N0}");

                    if (result.RecordsSkipped > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine($"Records Skipped: {result.RecordsSkipped:N0}");
                    }

                    if (result.RecordsFailed > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Records Failed: {result.RecordsFailed:N0}");
                    }

                    // Performance
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Batches Processed: {result.BatchesProcessed:N0}");
                    Console.WriteLine($"Processing Speed: {result.RecordsPerSecond:F2} records/sec");

                    // Success percentage
                    var successPercentage = result.GetSuccessPercentage();
                    Console.ForegroundColor = successPercentage >= 95 ? ConsoleColor.Green :
                                            successPercentage >= 80 ? ConsoleColor.Yellow : ConsoleColor.Red;
                    Console.WriteLine($"Success Rate: {successPercentage:F1}%");

                    // Source and Target
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"Source: {result.SourceIndex}");
                    Console.WriteLine($"Target: {result.TargetTable}");

                    // Warnings
                    if (result.Warnings.Any())
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"\n⚠️  Warnings ({result.Warnings.Count}):");
                        foreach (var warning in result.Warnings.Take(5))
                        {
                            Console.WriteLine($"   • {warning}");
                        }
                        if (result.Warnings.Count > 5)
                        {
                            Console.WriteLine($"   ... and {result.Warnings.Count - 5} more");
                        }
                    }

                    // Errors
                    if (result.Errors.Any())
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"\n❌ Errors ({result.Errors.Count}):");
                        foreach (var error in result.Errors.Take(3))
                        {
                            Console.WriteLine($"   • {error}");
                        }
                        if (result.Errors.Count > 3)
                        {
                            Console.WriteLine($"   ... and {result.Errors.Count - 3} more");
                        }
                    }

                    // Main error message
                    if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"\nMain Error: {result.ErrorMessage}");
                    }

                    Console.WriteLine();
                }
                finally
                {
                    Console.ForegroundColor = originalColor;
                }
            }
        }

        /// <summary>
        /// Ayarları güzel formatta gösterir
        /// </summary>
        public void ShowSettings(MigrationSettings settings)
        {
            lock (_lockObject)
            {
                var originalColor = Console.ForegroundColor;

                try
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
                    Console.WriteLine("║                    Migration Settings                       ║");
                    Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("ElasticSearch:");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"  Nodes: {string.Join(", ", settings.ElasticSearchNodes ?? Array.Empty<string>())}");
                    Console.WriteLine($"  Index: {settings.IndexName}");

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\nPostgreSQL:");
                    Console.ForegroundColor = ConsoleColor.White;
                    // Connection string'i güvenli göster (password'ü gizle)
                    var safeConnectionString = MaskPassword(settings.PostgreConnectionString);
                    Console.WriteLine($"  Connection: {safeConnectionString}");

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\nMigration:");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"  Batch Size: {settings.BatchSize:N0}");
                    Console.WriteLine($"  Scroll Timeout: {settings.ScrollTimeout}");
                    Console.WriteLine($"  Parallel Processing: {(settings.EnableParallelProcessing ? "Enabled" : "Disabled")}");
                    if (settings.EnableParallelProcessing)
                    {
                        Console.WriteLine($"  Max Parallelism: {settings.MaxDegreeOfParallelism}");
                    }
                    Console.WriteLine($"  Stop on Error: {settings.StopOnError}");
                    Console.WriteLine($"  Ignore Duplicates: {settings.IgnoreDuplicates}");
                    Console.WriteLine($"  Dry Run: {(settings.DryRun ? "Yes" : "No")}");
                    Console.WriteLine($"  Truncate Before: {settings.TruncateBeforeMigration}");

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\nLogging:");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"  Level: {settings.LogLevel}");

                    Console.WriteLine();
                }
                finally
                {
                    Console.ForegroundColor = originalColor;
                }
            }
        }

        /// <summary>
        /// Connection string'deki password'ü maskeler
        /// </summary>
        private static string MaskPassword(string? connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return "Not set";

            return System.Text.RegularExpressions.Regex.Replace(
                connectionString,
                @"Password=([^;]*)",
                "Password=***",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Başlık yazdırır
        /// </summary>
        public void WriteHeader(string title)
        {
            lock (_lockObject)
            {
                var originalColor = Console.ForegroundColor;

                try
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(new string('=', title.Length + 4));
                    Console.WriteLine($"  {title}");
                    Console.WriteLine(new string('=', title.Length + 4));
                    Console.WriteLine();
                }
                finally
                {
                    Console.ForegroundColor = originalColor;
                }
            }
        }

        /// <summary>
        /// Confirmation sorusu sorar
        /// </summary>
        public bool AskConfirmation(string question, bool defaultAnswer = false)
        {
            lock (_lockObject)
            {
                var originalColor = Console.ForegroundColor;

                try
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($"{question} ");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write(defaultAnswer ? "[Y/n]: " : "[y/N]: ");

                    Console.ForegroundColor = ConsoleColor.White;
                    var input = Console.ReadLine()?.Trim().ToLowerInvariant();

                    return input switch
                    {
                        "y" or "yes" => true,
                        "n" or "no" => false,
                        "" => defaultAnswer,
                        _ => defaultAnswer
                    };
                }
                finally
                {
                    Console.ForegroundColor = originalColor;
                }
            }
        }
    }
}
