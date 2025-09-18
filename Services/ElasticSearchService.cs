using Elasticsearch.Net;
using ElasticSearchPostgreSQLMigrationTool.Enums;
using ElasticSearchPostgreSQLMigrationTool.Interfaces;
using ElasticSearchPostgreSQLMigrationTool.Models;
using Nest;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using LogLevel = ElasticSearchPostgreSQLMigrationTool.Enums.LogLevel;

namespace ElasticSearchPostgreSQLMigrationTool.Services
{
    /// <summary>
    /// ElasticSearch operasyonları için servis implementasyonu
    /// </summary>
    public class ElasticSearchService : IElasticSearchService
    {
        private readonly ElasticClient _client;
        private readonly MigrationSettings _settings;
        private readonly ILogger _logger;

        public ElasticSearchService(MigrationSettings settings, ILogger logger)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // ElasticSearch ayarları kontrolü
            if (!_settings.IsElasticSearchConfigured())
            {
                throw new InvalidOperationException("ElasticSearch ayarları eksik veya geçersiz. ElasticSearchNodes ve IndexName belirtilmelidir.");
            }

            _client = CreateElasticClient();
        }

        /// <summary>
        /// ElasticSearch client'ını oluşturur
        /// </summary>
        private ElasticClient CreateElasticClient()
        {
            var connectionPool = new StaticConnectionPool(
                _settings.ElasticSearchNodes!.Select(node => new Uri(node))
            );

            var connectionSettings = new ConnectionSettings(connectionPool)
                .DefaultIndex(_settings.IndexName)
                .RequestTimeout(TimeSpan.FromMinutes(_settings.RequestTimeoutMinutes))
                .ServerCertificateValidationCallback((o, certificate, chain, errors) => true)
                .DisableDirectStreaming()
                .OnRequestCompleted(details =>
                {
                    if (details.DebugInformation != null && _settings.LogLevel == LogLevel.Debug)
                    {
                        _logger.LogDebug($"ES Request: {details.HttpMethod} {details.Uri}");
                    }
                });

            return new ElasticClient(connectionSettings);
        }

        /// <summary>
        /// ElasticSearch bağlantısını test eder
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                _logger.LogDebug("ElasticSearch bağlantısı test ediliyor...");

                var pingResponse = await _client.PingAsync();

                if (pingResponse.IsValid)
                {
                    _logger.LogInfo("ElasticSearch bağlantısı başarılı");
                    return true;
                }
                else
                {
                    _logger.LogError($"ElasticSearch ping başarısız: {pingResponse.DebugInformation}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ElasticSearch bağlantı testi sırasında hata");
                return false;
            }
        }

        /// <summary>
        /// Cluster sağlık durumunu kontrol eder
        /// </summary>
        public async Task<bool> IsClusterHealthyAsync()
        {
            try
            {
                var healthResponse = await _client.Cluster.HealthAsync();

                if (healthResponse.IsValid)
                {
                    var status = healthResponse.Status;
                    _logger.LogDebug($"Cluster health status: {status}");

                    // Green veya Yellow kabul edilebilir (Red kabul edilmez)
                    return status == Health.Green || status == Health.Yellow;
                }

                _logger.LogWarning($"Cluster health check başarısız: {healthResponse.DebugInformation}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cluster health check sırasında hata");
                return false;
            }
        }

        /// <summary>
        /// Toplam kayıt sayısını döndürür
        /// </summary>
        public async Task<long> GetTotalRecordCountAsync()
        {
            try
            {
                var countResponse = await _client.CountAsync<JObject>(c => c
                    .Index(_settings.IndexName)
                    .Query(q => q.MatchAll())
                );

                if (countResponse.IsValid)
                {
                    _logger.LogInfo($"Toplam kayıt sayısı: {countResponse.Count:N0}");
                    return countResponse.Count;
                }
                else
                {
                    _logger.LogError($"Kayıt sayısı alınamadı: {countResponse.DebugInformation}");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kayıt sayısı alınırken hata");
                return 0;
            }
        }

        /// <summary>
        /// ElasticSearch'den tüm access log kayıtlarını çeker
        /// </summary>
        public async Task<IEnumerable<AccessLog>> GetAllAccessLogsAsync()
        {
            var allLogs = new List<AccessLog>();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInfo("ElasticSearch'den veriler çekilmeye başlanıyor...");

                // İlk scroll search
                var searchResponse = await _client.SearchAsync<JObject>(s => s
                    .Index(_settings.IndexName)
                    .Size(_settings.BatchSize)
                    .Scroll(_settings.ScrollTimeout)
                    .Query(q => q.MatchAll())
                    .Sort(sort => sort.Field(f => f.Field("_doc"))) // Düzeltilmiş sort syntax
                );

                if (!searchResponse.IsValid)
                {
                    throw new InvalidOperationException($"ElasticSearch sorgusu başarısız: {searchResponse.DebugInformation}");
                }

                int batchNumber = 0;
                int totalProcessed = 0;

                // Scroll ile tüm verileri çek
                do
                {
                    batchNumber++;
                    var batchStopwatch = Stopwatch.StartNew();

                    _logger.LogProgress(totalProcessed, (int)(await GetTotalRecordCountAsync()),
                        $"Batch {batchNumber} işleniyor ({searchResponse.Documents.Count} kayıt)");

                    var batchLogs = new List<AccessLog>();

                    // Batch'teki her document'i AccessLog'a çevir
                    foreach (var doc in searchResponse.Documents)
                    {
                        try
                        {
                            var accessLog = MapToAccessLog(doc);
                            batchLogs.Add(accessLog);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Kayıt dönüştürme hatası: {ex.Message}");
                        }
                    }

                    allLogs.AddRange(batchLogs);
                    totalProcessed += batchLogs.Count;

                    batchStopwatch.Stop();
                    _logger.LogDebug($"Batch {batchNumber} {batchStopwatch.ElapsedMilliseconds}ms'de tamamlandı");

                    // Sonraki batch'i al
                    searchResponse = await _client.ScrollAsync<JObject>(_settings.ScrollTimeout, searchResponse.ScrollId);

                } while (searchResponse.IsValid && searchResponse.Documents.Any());

                // Scroll'u temizle
                if (!string.IsNullOrEmpty(searchResponse.ScrollId))
                {
                    await _client.ClearScrollAsync(c => c.ScrollId(searchResponse.ScrollId));
                }

                stopwatch.Stop();
                _logger.LogInfo($"ElasticSearch'den toplam {allLogs.Count:N0} kayıt çekildi ({stopwatch.Elapsed:hh\\:mm\\:ss})");

                return allLogs;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, $"ElasticSearch veri çekme işlemi {stopwatch.Elapsed:hh\\:mm\\:ss} sonra hata ile sonlandı");
                throw;
            }
        }

        /// <summary>
        /// ElasticSearch JSON document'ini AccessLog modeline çevirir
        /// </summary>
        private static AccessLog MapToAccessLog(JObject doc)
        {
            return new AccessLog
            {
                AccessLogFlag = GetFirstArrayValue<bool>(doc["accessLog"]),
                AreaName = GetFirstArrayValue<string>(doc["areaName"]),
                EventId = GetFirstArrayValue<int?>(doc["eventId"]),
                EventName = GetFirstArrayValue<string>(doc["eventName"]),
                GateName = GetFirstArrayValue<string>(doc["gateName"]),
                GksType = GetFirstArrayValue<string>(doc["gksType"]),
                Image = GetFirstArrayValue<string>(doc["image"]),
                Ip = GetFirstArrayValue<string>(doc["ip"]),
                IsAccreditation = GetFirstArrayValue<bool>(doc["isAccreditation"]),
                NationalityId = GetFirstArrayValue<string>(doc["nationalityId"]),
                PassageDuration = GetFirstArrayValue<decimal?>(doc["passageDuration"]),
                Port = GetFirstArrayValue<string>(doc["port"]),
                ReaderName = GetFirstArrayValue<string>(doc["readerName"]),
                Result = GetFirstArrayValue<string>(doc["result"]),
                SerialNumber = GetFirstArrayValue<string>(doc["serialNumber"]),
                StadiumId = GetFirstArrayValue<int?>(doc["stadiumId"]),
                Timestamp = ParseDateTime(GetFirstArrayValue<string>(doc["timestamp"])),
                TransactionId = GetFirstArrayValue<int?>(doc["transactionId"]),
                TransactionTime = ParseDateTime(GetFirstArrayValue<string>(doc["transactionTime"])),
                ElasticsearchId = doc["_id"]?.ToString(),
                ElasticsearchIndex = doc["_index"]?.ToString(),
                ElasticsearchScore = doc["_score"]?.Type == JTokenType.Null ? null : doc["_score"]?.ToObject<decimal?>(),
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Array içindeki ilk değeri alır
        /// </summary>
        private static T GetFirstArrayValue<T>(JToken? token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return default(T)!;

            if (token.Type == JTokenType.Array && token.HasValues)
            {
                return token.First()!.ToObject<T>()!;
            }

            return token.ToObject<T>()!;
        }

        /// <summary>
        /// String'i DateTime'a çevirir
        /// </summary>
        private static DateTime? ParseDateTime(string? dateString)
        {
            if (string.IsNullOrEmpty(dateString))
                return null;

            // ISO 8601 formatını tercih et
            if (DateTime.TryParse(dateString, out DateTime result))
            {
                return result;
            }

            // Unix timestamp deneme
            if (long.TryParse(dateString, out long unixTimestamp))
            {
                try
                {
                    return DateTimeOffset.FromUnixTimeMilliseconds(unixTimestamp).DateTime;
                }
                catch
                {
                    // Unix timestamp olarak parse edilemedi
                }
            }

            return null;
        }

        /// <summary>
        /// Kaynakları temizler
        /// </summary>
        public void Dispose()
        {
            // ElasticClient'ın IDisposable implement etmediği durumlarda null check yapıp geç
            try
            {
                // Eğer client disposal destekliyorsa (bu NEST versiyonuna bağlı)
                if (_client is IDisposable disposableClient)
                {
                    disposableClient.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"ElasticClient dispose edilirken hata: {ex.Message}");
            }
        }
    }
}