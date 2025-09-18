using ElasticSearchPostgreSQLMigrationTool.Interfaces;
using ElasticSearchPostgreSQLMigrationTool.Enums;
using ElasticSearchPostgreSQLMigrationTool.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticsearchPostgreSQLMigrationTool.Services
{
    public class CSVService : ICSVService
    {
        private readonly ILogger _logger;
        private readonly MigrationSettings _settings;

        // CSV column mappings - ElasticSearch export formatına göre
        private static readonly Dictionary<string, string> ColumnMappings = new()
        {
            ["_id"] = "ElasticsearchId",
            ["_index"] = "ElasticsearchIndex",
            ["_score"] = "ElasticsearchScore",
            ["accessLog"] = "AccessLogFlag",
            ["areaName"] = "AreaName",
            ["eventId"] = "EventId",
            ["eventName"] = "EventName",
            ["gateName"] = "GateName",
            ["gksType"] = "GksType",
            ["image"] = "Image",
            ["ip"] = "Ip",
            ["isAccreditation"] = "IsAccreditation",
            ["nationalityId"] = "NationalityId",
            ["passageDuration"] = "PassageDuration",
            ["port"] = "Port",
            ["readerName"] = "ReaderName",
            ["result"] = "Result",
            ["serialNumber"] = "SerialNumber",
            ["stadiumId"] = "StadiumId",
            ["timestamp"] = "Timestamp",
            ["transactionId"] = "TransactionId",
            ["transactionTime"] = "TransactionTime"
        };

        public CSVService(ILogger logger, MigrationSettings settings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        #region ICSVService Implementation (Existing Methods)

        /// <summary>
        /// CSV dosyasını validate eder
        /// </summary>
        public async Task<bool> ValidateFileAsync(string filePath)
        {
            try
            {
                _logger.LogDebug($"CSV dosyası validate ediliyor: {filePath}");

                if (string.IsNullOrEmpty(filePath))
                {
                    _logger.LogError("Dosya yolu boş");
                    return false;
                }

                if (!File.Exists(filePath))
                {
                    _logger.LogError($"Dosya bulunamadı: {filePath}");
                    return false;
                }

                var fileInfo = new FileInfo(filePath);

                if (fileInfo.Length == 0)
                {
                    _logger.LogError("Dosya boş");
                    return false;
                }

                _logger.LogInfo($"CSV dosyası geçerli - Boyut: {fileInfo.Length:N0} bytes");

                // Header kontrolü
                using var reader = new StreamReader(filePath);
                var firstLine = await reader.ReadLineAsync();

                if (string.IsNullOrEmpty(firstLine))
                {
                    _logger.LogError("CSV dosyası header içermiyor");
                    return false;
                }

                var headers = ParseCSVLine(firstLine);
                var requiredHeaders = new[] { "_id", "accessLog", "eventId", "timestamp" };
                var missingHeaders = requiredHeaders.Where(h => !headers.Contains(h, StringComparer.OrdinalIgnoreCase)).ToList();

                if (missingHeaders.Any())
                {
                    _logger.LogError($"Gerekli header'lar eksik: {string.Join(", ", missingHeaders)}");
                    return false;
                }

                _logger.LogInfo($"CSV header geçerli - {headers.Length} kolon bulundu");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CSV dosya validation hatası");
                return false;
            }
        }

        /// <summary>
        /// CSV dosyasındaki kayıt sayısını döndürür
        /// </summary>
        public async Task<long> GetRecordCountAsync(string filePath)
        {
            try
            {
                using var reader = new StreamReader(filePath);

                // Header'ı atla
                await reader.ReadLineAsync();

                long count = 0;
                while (await reader.ReadLineAsync() != null)
                {
                    count++;
                }

                _logger.LogInfo($"CSV dosyasında {count:N0} veri satırı bulundu");
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kayıt sayısı hesaplama hatası");
                return 0;
            }
        }

        /// <summary>
        /// CSV dosyasını analiz eder
        /// </summary>
        public async Task<CSVAnalysisResult> AnalyzeCSVAsync(string filePath)
        {
            var result = new CSVAnalysisResult
            {
                FilePath = filePath,
                AnalysisTime = DateTime.UtcNow
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInfo($"CSV analizi başlıyor: {filePath}");

                var fileInfo = new FileInfo(filePath);
                result.FileSizeBytes = fileInfo.Length;
                result.FileSizeMB = Math.Round(fileInfo.Length / 1024.0 / 1024.0, 2);

                using var reader = new StreamReader(filePath);

                // Header analizi
                var headerLine = await reader.ReadLineAsync();
                if (!string.IsNullOrEmpty(headerLine))
                {
                    result.Headers = ParseCSVLine(headerLine);
                    result.ColumnCount = result.Headers.Length;
                    result.RecognizedColumns = result.Headers.Where(h => ColumnMappings.ContainsKey(h)).ToArray();
                    result.UnrecognizedColumns = result.Headers.Where(h => !ColumnMappings.ContainsKey(h)).ToArray();
                }

                // Sample veri analizi
                var sampleLines = new List<string>();
                var sampleSize = Math.Min(100, _settings.BatchSize / 10);

                for (int i = 0; i < sampleSize; i++)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null) break;
                    sampleLines.Add(line);
                }

                result.SampleSize = sampleLines.Count;

                if (sampleLines.Any())
                {
                    // Sample validation
                    var validCount = 0;
                    var errors = new List<string>();

                    foreach (var line in sampleLines)
                    {
                        try
                        {
                            var accessLog = ParseCSVLineToAccessLog(line, result.Headers);
                            if (accessLog != null)
                            {
                                validCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            errors.Add(ex.Message);
                        }
                    }

                    result.ValidSampleCount = validCount;
                    result.InvalidSampleCount = result.SampleSize - validCount;
                    result.SampleErrors = errors.Take(10).ToArray(); // İlk 10 hata
                    result.EstimatedValidPercentage = result.SampleSize > 0 ? (double)validCount / result.SampleSize * 100 : 0;
                }

                // Toplam kayıt sayısı (dosya sonuna git)
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                result.TotalRecords = await GetRecordCountAsync(filePath);
                result.EstimatedValidRecords = (long)(result.TotalRecords * result.EstimatedValidPercentage / 100);

                result.IsValid = result.EstimatedValidPercentage >= 50; // %50'den fazlası geçerliyse OK

                stopwatch.Stop();
                result.AnalysisDuration = stopwatch.Elapsed;

                _logger.LogInfo($"CSV analizi tamamlandı ({stopwatch.ElapsedMilliseconds}ms)");
                _logger.LogInfo($"  Toplam kayıt: {result.TotalRecords:N0}");
                _logger.LogInfo($"  Geçerli tahmini: {result.EstimatedValidPercentage:F1}%");
                _logger.LogInfo($"  Tanınan kolon: {result.RecognizedColumns?.Length}/{result.ColumnCount}");

                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.AnalysisError = ex.Message;
                _logger.LogError(ex, "CSV analiz hatası");
                return result;
            }
        }

        /// <summary>
        /// CSV dosyasından tüm access log kayıtlarını okur
        /// </summary>
        public async Task<IEnumerable<AccessLog>> ReadAccessLogsAsync(string filePath)
        {
            var logs = new List<AccessLog>();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInfo($"CSV dosyası okunuyor: {filePath}");

                using var reader = new StreamReader(filePath);

                // Header'ı oku
                var headerLine = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(headerLine))
                {
                    throw new InvalidOperationException("CSV dosyası header içermiyor");
                }

                var headers = ParseCSVLine(headerLine);
                _logger.LogDebug($"CSV headers: {string.Join(", ", headers)}");

                string? line;
                int lineNumber = 1; // Header = 1, data = 2'den başlıyor
                int processedCount = 0;
                int errorCount = 0;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lineNumber++;

                    try
                    {
                        var accessLog = ParseCSVLineToAccessLog(line, headers);
                        if (accessLog != null)
                        {
                            logs.Add(accessLog);
                            processedCount++;

                            // Progress logging (her 1000 kayıtta bir)
                            if (processedCount % 1000 == 0)
                            {
                                _logger.LogProgress(processedCount, (int)await GetRecordCountAsync(filePath),
                                    $"CSV okunuyor - {processedCount:N0} kayıt işlendi");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;

                        if (_settings.LogLevel >= LogLevel.Debug)
                        {
                            _logger.LogWarning($"Satır {lineNumber} parse edilemedi: {ex.Message}");
                        }

                        if (_settings.StopOnError && errorCount > 10)
                        {
                            throw new InvalidOperationException($"Çok fazla parse hatası ({errorCount}). İşlem durduruldu.");
                        }
                    }
                }

                stopwatch.Stop();

                _logger.LogInfo($"CSV okuma tamamlandı:");
                _logger.LogInfo($"  İşlenen kayıt: {processedCount:N0}");
                _logger.LogInfo($"  Hatalı kayıt: {errorCount:N0}");
                _logger.LogInfo($"  Süre: {stopwatch.Elapsed:hh\\:mm\\:ss}");
                _logger.LogInfo($"  Hız: {(processedCount / stopwatch.Elapsed.TotalSeconds):F0} kayıt/saniye");

                if (errorCount > 0)
                {
                    var errorPercentage = (double)errorCount / (processedCount + errorCount) * 100;
                    _logger.LogWarning($"  Hata oranı: {errorPercentage:F1}%");
                }

                return logs;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, $"CSV okuma hatası ({stopwatch.Elapsed:hh\\:mm\\:ss} sonra)");
                throw;
            }
        }

        /// <summary>
        /// CSV dosyasının header bilgilerini döner
        /// </summary>
        public async Task<IEnumerable<string>> GetHeadersAsync(string csvFilePath)
        {
            try
            {
                using var reader = new StreamReader(csvFilePath);
                string? headerLine = await reader.ReadLineAsync();

                if (string.IsNullOrEmpty(headerLine))
                {
                    return Enumerable.Empty<string>();
                }

                return ParseCSVLine(headerLine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"CSV header okuma hatası: {csvFilePath}");
                throw;
            }
        }

        #endregion

        #region ICsvReaderService Implementation (Hibrit Interface)

        /// <summary>
        /// CSV dosyasını okur ve AccessLog listesi döner (Hibrit yaklaşım için alias)
        /// </summary>
        public async Task<IEnumerable<AccessLog>> ReadCsvAsync(string csvFilePath)
        {
            return await ReadAccessLogsAsync(csvFilePath);
        }

        /// <summary>
        /// CSV dosyasındaki toplam satır sayısını döner (Hibrit yaklaşım için int dönüş)
        /// </summary>
        public async Task<int> GetRecordCountIntAsync(string csvFilePath)
        {
            var count = await GetRecordCountAsync(csvFilePath);
            return (int)Math.Min(count, int.MaxValue); // long'dan int'e güvenli dönüşüm
        }

        /// <summary>
        /// CSV dosyasının geçerli format kontrolünü yapar (ValidateFileAsync'in alias'ı)
        /// </summary>
        public async Task<bool> ValidateFormatAsync(string csvFilePath)
        {
            return await ValidateFileAsync(csvFilePath);
        }

        #endregion

        #region Private Methods (Existing)

        /// <summary>
        /// CSV satırını parse eder
        /// </summary>
        private static string[] ParseCSVLine(string line)
        {
            var fields = new List<string>();
            bool inQuotes = false;
            var currentField = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // Escaped quote
                        currentField.Append('"');
                        i++; // Skip next quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(currentField.ToString().Trim());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            fields.Add(currentField.ToString().Trim());
            return fields.ToArray();
        }

        /// <summary>
        /// CSV satırını AccessLog nesnesine çevirir
        /// </summary>
        private AccessLog? ParseCSVLineToAccessLog(string line, string[] headers)
        {
            var fields = ParseCSVLine(line);

            if (fields.Length != headers.Length)
            {
                throw new InvalidOperationException($"Alan sayısı eşleşmiyor. Beklenen: {headers.Length}, Bulunan: {fields.Length}");
            }

            var accessLog = new AccessLog
            {
                CreatedAt = DateTime.UtcNow
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var header = headers[i];
                var value = fields[i];

                try
                {
                    MapCSVFieldToAccessLog(accessLog, header, value);
                }
                catch (Exception ex)
                {
                    if (_settings.LogLevel >= LogLevel.Debug)
                    {
                        _logger.LogWarning($"Alan mapping hatası - {header}: {value} -> {ex.Message}");
                    }
                    // Field mapping hatalarını ignore et, devam et
                }
            }

            // Zorunlu alanları kontrol et
            if (string.IsNullOrEmpty(accessLog.ElasticsearchId))
            {
                throw new InvalidOperationException("ElasticSearch ID bulunamadı");
            }

            return accessLog;
        }

        /// <summary>
        /// CSV field'ını AccessLog property'sine map eder
        /// </summary>
        private void MapCSVFieldToAccessLog(AccessLog accessLog, string csvHeader, string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Equals("null", StringComparison.OrdinalIgnoreCase))
                return;

            // Boolean alanlar için özel handling
            if (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1")
                value = "true";
            else if (value.Equals("false", StringComparison.OrdinalIgnoreCase) || value == "0")
                value = "false";

            switch (csvHeader.ToLowerInvariant())
            {
                case "_id":
                    accessLog.ElasticsearchId = value;
                    break;
                case "_index":
                    accessLog.ElasticsearchIndex = value;
                    break;
                case "_score":
                    if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var score))
                        accessLog.ElasticsearchScore = score;
                    break;
                case "accesslog":
                    accessLog.AccessLogFlag = bool.TryParse(value, out var accessLogFlag) && accessLogFlag;
                    break;
                case "areaname":
                    accessLog.AreaName = value;
                    break;
                case "eventid":
                    if (int.TryParse(value, out var eventId))
                        accessLog.EventId = eventId;
                    break;
                case "eventname":
                    accessLog.EventName = value;
                    break;
                case "gatename":
                    accessLog.GateName = value;
                    break;
                case "gkstype":
                    accessLog.GksType = value;
                    break;
                case "image":
                    accessLog.Image = value;
                    break;
                case "ip":
                    accessLog.Ip = value;
                    break;
                case "isaccreditation":
                    // CSV'de string olarak gelebilir
                    if (bool.TryParse(value, out var isAccreditation))
                        accessLog.IsAccreditation = isAccreditation;
                    else if (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1")
                        accessLog.IsAccreditation = true;
                    break;
                case "nationalityid":
                    accessLog.NationalityId = value;
                    break;
                case "passageduration":
                    if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var duration))
                        accessLog.PassageDuration = duration;
                    break;
                case "port":
                    accessLog.Port = value;
                    break;
                case "readername":
                    accessLog.ReaderName = value;
                    break;
                case "result":
                    accessLog.Result = value;
                    break;
                case "serialnumber":
                    accessLog.SerialNumber = value;
                    break;
                case "stadiumid":
                    if (int.TryParse(value, out var stadiumId))
                        accessLog.StadiumId = stadiumId;
                    break;
                case "timestamp":
                    accessLog.Timestamp = ParseDateTime(value);
                    break;
                case "transactionid":
                    if (int.TryParse(value, out var transactionId))
                        accessLog.TransactionId = transactionId;
                    break;
                case "transactiontime":
                    accessLog.TransactionTime = ParseDateTime(value);
                    break;
                default:
                    // Bilinmeyen header - debug log
                    if (_settings.LogLevel >= LogLevel.Debug)
                    {
                        _logger.LogDebug($"Bilinmeyen CSV header: {csvHeader}");
                    }
                    break;
            }
        }

        /// <summary>
        /// String'i DateTime'a çevirir (çeşitli formatları destekler)
        /// </summary>
        private static DateTime? ParseDateTime(string? dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
                return null;

            // ISO 8601 formatı
            if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
            {
                return result;
            }

            // Unix timestamp (milliseconds)
            if (long.TryParse(dateString, out var unixTimestamp))
            {
                try
                {
                    if (unixTimestamp > 1000000000000) // Milliseconds
                    {
                        return DateTimeOffset.FromUnixTimeMilliseconds(unixTimestamp).DateTime;
                    }
                    else // Seconds
                    {
                        return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).DateTime;
                    }
                }
                catch
                {
                    // Unix timestamp olarak parse edilemedi
                }
            }

            // Diğer yaygın formatları dene
            var formats = new[]
            {
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-dd HH:mm:ss.fff",
                "yyyy-MM-ddTHH:mm:ss",
                "yyyy-MM-ddTHH:mm:ss.fff",
                "yyyy-MM-ddTHH:mm:ssZ",
                "yyyy-MM-ddTHH:mm:ss.fffZ",
                "dd/MM/yyyy HH:mm:ss",
                "MM/dd/yyyy HH:mm:ss"
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                {
                    return result;
                }
            }

            return null;
        }

        #endregion
    }
}