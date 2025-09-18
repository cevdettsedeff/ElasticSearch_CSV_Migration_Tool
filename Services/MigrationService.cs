using ElasticSearchPostgreSQLMigrationTool.Interfaces;
using ElasticSearchPostgreSQLMigrationTool.Enums;
using ElasticSearchPostgreSQLMigrationTool.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ElasticSearchPostgreSQLMigrationTool.Services
{
    /// <summary>
    /// Migration işlemleri için hibrit servis implementasyonu
    /// Hem ElasticSearch hem de CSV kaynaklarını destekler
    /// </summary>
    public class MigrationService : IMigrationService
    {
        private readonly IElasticSearchService _elasticSearchService;
        private readonly ICSVService _csvService;
        private readonly IPostgreSQLService _postgreSQLService;
        private readonly IValidationService _validationService;
        private readonly MigrationSettings _settings;
        private readonly ILogger _logger;

        public event EventHandler<MigrationProgressEventArgs>? ProgressChanged;

        public MigrationService(
            IElasticSearchService elasticSearchService,
            ICSVService csvService,
            IPostgreSQLService postgreSQLService,
            IValidationService validationService,
            MigrationSettings settings,
            ILogger logger)
        {
            _elasticSearchService = elasticSearchService ?? throw new ArgumentNullException(nameof(elasticSearchService));
            _csvService = csvService ?? throw new ArgumentNullException(nameof(csvService));
            _postgreSQLService = postgreSQLService ?? throw new ArgumentNullException(nameof(postgreSQLService));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region ElasticSearch Migration Methods

        /// <summary>
        /// ElasticSearch'den PostgreSQL'e migration işlemini başlatır ve tamamlar
        /// </summary>
        public async Task<MigrationResult> MigrateFromElasticSearchAsync()
        {
            var result = new MigrationResult
            {
                StartTime = DateTime.UtcNow,
                SourceType = DataSourceType.ElasticSearch,
                SourceIndex = _settings.IndexName,
                BatchSize = _settings.BatchSize
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInfo("🚀 ElasticSearch migration işlemi başlatılıyor...");

                // 1. Ön kontroller
                await PerformElasticSearchPreChecksAsync(result);

                // 2. Kaynak veri sayısını öğren
                result.TotalRecordsInSource = (int)await _elasticSearchService.GetTotalRecordCountAsync();
                _logger.LogInfo($"📊 Toplam kaynak veri: {result.TotalRecordsInSource:N0} kayıt");

                // 3. Dry run kontrolü
                if (_settings.DryRun)
                {
                    return await PerformElasticSearchDryRunAsync(result);
                }

                // 4. Hedef tabloyu hazırla
                await PrepareTargetTableAsync(result);

                // 5. Ana migration işlemi
                await PerformElasticSearchMigrationAsync(result, stopwatch);

                result.Success = true;
                _logger.LogInfo("✅ ElasticSearch migration başarıyla tamamlandı!");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.Errors.Add(ex.Message);
                _logger.LogError(ex, "❌ ElasticSearch migration sırasında kritik hata");
            }
            finally
            {
                stopwatch.Stop();
                result.Complete();
            }

            return result;
        }

        /// <summary>
        /// Dry run modunda ElasticSearch migration analizi yapar
        /// </summary>
        public async Task<MigrationResult> AnalyzeElasticSearchAsync()
        {
            var result = new MigrationResult
            {
                StartTime = DateTime.UtcNow,
                SourceType = DataSourceType.ElasticSearch,
                SourceIndex = _settings.IndexName,
                BatchSize = _settings.BatchSize
            };

            try
            {
                _logger.LogInfo("🧪 ElasticSearch Dry Run modu - Sadece analiz yapılıyor...");

                // ElasticSearch bağlantı kontrolü
                if (!await _elasticSearchService.TestConnectionAsync())
                {
                    throw new InvalidOperationException("ElasticSearch bağlantısı başarısız");
                }

                // Küçük sample al
                var sampleLogs = (await _elasticSearchService.GetAllAccessLogsAsync())
                    .Take(Math.Min(1000, _settings.BatchSize))
                    .ToList();

                if (sampleLogs.Any())
                {
                    var analysisResult = AnalyzeDataSample(sampleLogs, result);
                    _logger.LogInfo($"📊 ElasticSearch Sample Analysis tamamlandı");
                    return analysisResult;
                }

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "ElasticSearch dry run sırasında hata");
            }

            result.Complete();
            return result;
        }

        #endregion

        #region CSV Migration Methods

        /// <summary>
        /// CSV dosyasından PostgreSQL'e migration işlemini başlatır ve tamamlar
        /// </summary>
        public async Task<MigrationResult> MigrateFromCSVAsync(string csvFilePath)
        {
            var result = new MigrationResult
            {
                StartTime = DateTime.UtcNow,
                SourceType = DataSourceType.CSV,
                SourceFile = csvFilePath,
                BatchSize = _settings.BatchSize
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInfo($"🚀 CSV migration işlemi başlatılıyor: {csvFilePath}");

                // 1. Ön kontroller
                await PerformCsvPreChecksAsync(csvFilePath, result);

                // 2. CSV'yi oku
                var csvData = await _csvService.ReadCsvAsync(csvFilePath);
                var accessLogs = csvData.ToList();

                result.TotalRecordsInSource = accessLogs.Count;
                _logger.LogInfo($"📊 CSV'den okunan kayıt sayısı: {result.TotalRecordsInSource:N0}");

                // 3. Dry run kontrolü
                if (_settings.DryRun)
                {
                    return await PerformCsvDryRunAsync(accessLogs, result);
                }

                // 4. Hedef tabloyu hazırla
                await PrepareTargetTableAsync(result);

                // 5. Ana migration işlemi
                await PerformCsvMigrationAsync(accessLogs, result, stopwatch);

                result.Success = true;
                _logger.LogInfo("✅ CSV migration başarıyla tamamlandı!");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.Errors.Add(ex.Message);
                _logger.LogError(ex, "❌ CSV migration sırasında kritik hata");
            }
            finally
            {
                stopwatch.Stop();
                result.Complete();
            }

            return result;
        }

        /// <summary>
        /// Dry run modunda CSV migration analizi yapar
        /// </summary>
        public async Task<MigrationResult> AnalyzeCSVAsync(string csvFilePath)
        {
            var result = new MigrationResult
            {
                StartTime = DateTime.UtcNow,
                SourceType = DataSourceType.CSV,
                SourceFile = csvFilePath,
                BatchSize = _settings.BatchSize
            };

            try
            {
                _logger.LogInfo($"🧪 CSV Dry Run modu - Sadece analiz yapılıyor: {csvFilePath}");

                // Dosya varlık kontrolü
                if (!System.IO.File.Exists(csvFilePath))
                {
                    throw new System.IO.FileNotFoundException($"CSV dosyası bulunamadı: {csvFilePath}");
                }

                // CSV'yi oku (sample)
                var csvData = await _csvService.ReadCsvAsync(csvFilePath);
                var sampleLogs = csvData.Take(Math.Min(1000, _settings.BatchSize)).ToList();

                if (sampleLogs.Any())
                {
                    var analysisResult = AnalyzeDataSample(sampleLogs, result);
                    _logger.LogInfo($"📊 CSV Sample Analysis tamamlandı");
                    return analysisResult;
                }

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "CSV dry run sırasında hata");
            }

            result.Complete();
            return result;
        }

        #endregion

        #region Shared Methods

        /// <summary>
        /// Ortak veri analizi yapar
        /// </summary>
        private MigrationResult AnalyzeDataSample(List<AccessLog> sampleData, MigrationResult result)
        {
            // Sample validation
            var validationResults = _validationService.ValidateBatch(sampleData).ToList();
            var stats = ((ValidationService)_validationService).GetBatchValidationStats(validationResults);

            result.TotalRecordsProcessed = stats.TotalCount;
            result.RecordsInserted = stats.ValidCount;
            result.RecordsFailed = stats.InvalidCount;

            _logger.LogInfo($"📊 Sample Analysis ({stats.TotalCount} kayıt):");
            _logger.LogInfo($"   ✅ Geçerli: {stats.ValidCount} ({stats.ValidPercentage:F1}%)");
            _logger.LogInfo($"   ❌ Geçersiz: {stats.InvalidCount} ({stats.InvalidPercentage:F1}%)");
            _logger.LogInfo($"   ⚠️ Uyarılı: {stats.WarningCount} ({stats.WarningPercentage:F1}%)");

            if (stats.MostCommonErrors.Any())
            {
                _logger.LogInfo("🔍 En Sık Hatalar:");
                foreach (var error in stats.MostCommonErrors.Take(5))
                {
                    _logger.LogInfo($"   • {error.Key}: {error.Value} kez");
                }
            }

            result.Success = true;
            return result;
        }

        /// <summary>
        /// Hedef tabloyu hazırlar
        /// </summary>
        private async Task PrepareTargetTableAsync(MigrationResult result)
        {
            _logger.LogInfo("🔧 Hedef tablo hazırlanıyor...");

            // PostgreSQL bağlantı kontrolü
            if (!await _postgreSQLService.TestConnectionAsync())
            {
                throw new InvalidOperationException("PostgreSQL bağlantısı başarısız");
            }

            // Mevcut kayıt sayısını al
            if (await _postgreSQLService.TableExistsAsync())
            {
                result.RecordsBeforeMigration = (int)await _postgreSQLService.GetRecordCountAsync();
                _logger.LogInfo($"📊 Mevcut tablo kayıt sayısı: {result.RecordsBeforeMigration:N0}");

                // Truncate kontrolü
                if (_settings.TruncateBeforeMigration)
                {
                    _logger.LogWarning("⚠️ Tablo temizleniyor...");
                    await _postgreSQLService.TruncateTableAsync();
                    result.RecordsBeforeMigration = 0;
                }
            }
            else
            {
                _logger.LogInfo("📝 Tablo oluşturuluyor...");
                await _postgreSQLService.CreateTableAsync();
            }
        }

        #endregion

        #region ElasticSearch Specific Methods

        private async Task PerformElasticSearchPreChecksAsync(MigrationResult result)
        {
            _logger.LogInfo("🔍 ElasticSearch ön kontrolleri yapılıyor...");

            // ElasticSearch bağlantı kontrolü
            if (!await _elasticSearchService.TestConnectionAsync())
            {
                throw new InvalidOperationException("ElasticSearch bağlantısı başarısız");
            }

            // Cluster health kontrolü
            if (!await _elasticSearchService.IsClusterHealthyAsync())
            {
                result.Warnings.Add("ElasticSearch cluster sağlığı optimal değil");
                _logger.LogWarning("⚠️ ElasticSearch cluster sağlığı optimal değil, devam ediliyor...");
            }

            _logger.LogInfo("✅ ElasticSearch ön kontrolleri başarılı");
        }

        private async Task<MigrationResult> PerformElasticSearchDryRunAsync(MigrationResult result)
        {
            _logger.LogInfo("🧪 ElasticSearch Dry Run modu - Sadece analiz yapılıyor...");

            try
            {
                // Küçük sample al
                var sampleLogs = (await _elasticSearchService.GetAllAccessLogsAsync())
                    .Take(Math.Min(1000, _settings.BatchSize))
                    .ToList();

                if (sampleLogs.Any())
                {
                    return AnalyzeDataSample(sampleLogs, result);
                }

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "ElasticSearch dry run sırasında hata");
            }

            return result;
        }

        private async Task PerformElasticSearchMigrationAsync(MigrationResult result, Stopwatch stopwatch)
        {
            _logger.LogInfo("🔄 ElasticSearch'den veri aktarımı başlıyor...");

            // ElasticSearch'den verileri çek
            var allLogs = await _elasticSearchService.GetAllAccessLogsAsync();
            var logList = allLogs.ToList();

            await ProcessDataMigration(logList, result, stopwatch);
        }

        #endregion

        #region CSV Specific Methods

        private async Task PerformCsvPreChecksAsync(string csvFilePath, MigrationResult result)
        {
            _logger.LogInfo("🔍 CSV ön kontrolleri yapılıyor...");

            // CSV service ile validation
            if (!await _csvService.ValidateFormatAsync(csvFilePath))
            {
                throw new InvalidOperationException("CSV dosyası format doğrulamasından geçemedi");
            }

            // Dosya boyut kontrolü
            var fileInfo = new System.IO.FileInfo(csvFilePath);
            _logger.LogInfo($"📁 CSV dosya boyutu: {fileInfo.Length / 1024 / 1024:F2} MB");

            _logger.LogInfo("✅ CSV ön kontrolleri başarılı");
        }

        private async Task<MigrationResult> PerformCsvDryRunAsync(List<AccessLog> accessLogs, MigrationResult result)
        {
            _logger.LogInfo("🧪 CSV Dry Run modu - Sadece analiz yapılıyor...");

            try
            {
                var sampleLogs = accessLogs.Take(Math.Min(1000, _settings.BatchSize)).ToList();

                if (sampleLogs.Any())
                {
                    return AnalyzeDataSample(sampleLogs, result);
                }

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "CSV dry run sırasında hata");
            }

            return result;
        }

        private async Task PerformCsvMigrationAsync(List<AccessLog> accessLogs, MigrationResult result, Stopwatch stopwatch)
        {
            _logger.LogInfo("🔄 CSV'den veri aktarımı başlıyor...");

            await ProcessDataMigration(accessLogs, result, stopwatch);
        }

        #endregion

        #region Common Processing Methods

        /// <summary>
        /// Ortak veri migration işlemi
        /// </summary>
        private async Task ProcessDataMigration(List<AccessLog> logList, MigrationResult result, Stopwatch stopwatch)
        {
            if (!logList.Any())
            {
                _logger.LogWarning("⚠️ Aktarılacak veri bulunamadı");
                return;
            }

            // Batch'lere böl
            var batches = SplitIntoBatches(logList, _settings.BatchSize);
            var totalBatches = batches.Count;

            _logger.LogInfo($"📦 Toplam {logList.Count:N0} kayıt {totalBatches} batch'e bölündü");

            // Paralel işlem kontrolü
            if (_settings.EnableParallelProcessing && totalBatches > 1)
            {
                await ProcessBatchesInParallelAsync(batches, result, stopwatch);
            }
            else
            {
                await ProcessBatchesSequentiallyAsync(batches, result, stopwatch);
            }

            // Son kayıt sayısını al
            result.RecordsAfterMigration = (int)await _postgreSQLService.GetRecordCountAsync();

            // Duplicate temizleme
            if (_settings.IgnoreDuplicates)
            {
                _logger.LogInfo("🧹 Duplicate kayıtlar temizleniyor...");
                var duplicatesRemoved = await _postgreSQLService.CleanupDuplicatesAsync();
                if (duplicatesRemoved > 0)
                {
                    result.RecordsSkipped += duplicatesRemoved;
                    result.Warnings.Add($"{duplicatesRemoved} duplicate kayıt temizlendi");
                }
            }
        }

        /// <summary>
        /// Batch'leri sıralı olarak işler
        /// </summary>
        private async Task ProcessBatchesSequentiallyAsync(List<List<AccessLog>> batches, MigrationResult result, Stopwatch overallStopwatch)
        {
            for (int i = 0; i < batches.Count; i++)
            {
                var batch = batches[i];
                var batchStopwatch = Stopwatch.StartNew();

                try
                {
                    await ProcessSingleBatchAsync(batch, i + 1, batches.Count, result);
                    result.BatchesProcessed++;
                }
                catch (Exception ex)
                {
                    result.AddBatchError(i + 1, ex.Message);

                    if (_settings.StopOnError)
                    {
                        throw new InvalidOperationException($"Batch {i + 1} hatası: {ex.Message}", ex);
                    }
                }
                finally
                {
                    batchStopwatch.Stop();
                    result.AddBatchDuration(i + 1, batchStopwatch.Elapsed);

                    // Progress event
                    OnProgressChanged(new MigrationProgressEventArgs
                    {
                        CurrentBatch = i + 1,
                        TotalBatches = batches.Count,
                        RecordsInBatch = batch.Count,
                        TotalRecordsProcessed = result.TotalRecordsProcessed,
                        Message = $"Batch {i + 1}/{batches.Count} tamamlandı",
                        Elapsed = overallStopwatch.Elapsed,
                        EstimatedTimeRemaining = CalculateEstimatedTimeRemaining(i + 1, batches.Count, overallStopwatch.Elapsed)
                    });
                }
            }
        }

        /// <summary>
        /// Batch'leri paralel olarak işler
        /// </summary>
        private async Task ProcessBatchesInParallelAsync(List<List<AccessLog>> batches, MigrationResult result, Stopwatch overallStopwatch)
        {
            _logger.LogInfo($"⚡ Paralel işlem başlıyor (Max: {_settings.MaxDegreeOfParallelism} thread)");

            var semaphore = new SemaphoreSlim(_settings.MaxDegreeOfParallelism);
            var tasks = new List<Task>();

            for (int i = 0; i < batches.Count; i++)
            {
                var batchIndex = i;
                var batch = batches[i];

                var task = Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    var batchStopwatch = Stopwatch.StartNew();

                    try
                    {
                        await ProcessSingleBatchAsync(batch, batchIndex + 1, batches.Count, result);
                        lock (result)
                        {
                            result.BatchesProcessed++;
                        }
                    }
                    catch (Exception ex)
                    {
                        result.AddBatchError(batchIndex + 1, ex.Message);

                        if (_settings.StopOnError)
                        {
                            throw;
                        }
                    }
                    finally
                    {
                        batchStopwatch.Stop();
                        result.AddBatchDuration(batchIndex + 1, batchStopwatch.Elapsed);
                        semaphore.Release();

                        // Thread-safe progress update
                        OnProgressChanged(new MigrationProgressEventArgs
                        {
                            CurrentBatch = result.BatchesProcessed,
                            TotalBatches = batches.Count,
                            RecordsInBatch = batch.Count,
                            TotalRecordsProcessed = result.TotalRecordsProcessed,
                            Message = $"Batch {batchIndex + 1}/{batches.Count} tamamlandı (Paralel)",
                            Elapsed = overallStopwatch.Elapsed
                        });
                    }
                });

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Tek bir batch'i işler
        /// </summary>
        private async Task ProcessSingleBatchAsync(List<AccessLog> batch, int batchNumber, int totalBatches, MigrationResult result)
        {
            _logger.LogDebug($"🔄 Batch {batchNumber}/{totalBatches} işleniyor ({batch.Count} kayıt)");

            // Validation (opsiyonel)
            if (_settings.LogLevel >= LogLevel.Debug)
            {
                var validationResults = _validationService.ValidateBatch(batch).ToList();
                var validCount = validationResults.Count(r => r.IsValid);
                var invalidCount = validationResults.Count(r => !r.IsValid);

                if (invalidCount > 0)
                {
                    result.Warnings.Add($"Batch {batchNumber}: {invalidCount} geçersiz kayıt bulundu");

                    if (_settings.StopOnError)
                    {
                        throw new InvalidOperationException($"Batch {batchNumber}'te {invalidCount} geçersiz kayıt");
                    }

                    // Geçerli kayıtları al
                    batch = validationResults.Where(r => r.IsValid)
                                           .Select(r => (AccessLog)r.ValidatedObject!)
                                           .ToList();
                }
            }

            // PostgreSQL'e insert
            var insertedCount = await _postgreSQLService.InsertBatchAsync(batch);

            // İstatistikleri güncelle (lock kullanarak thread-safe)
            lock (result)
            {
                result.TotalRecordsProcessed += batch.Count;
                result.RecordsInserted += insertedCount;
                result.RecordsSkipped += batch.Count - insertedCount;
            }

            _logger.LogDebug($"✅ Batch {batchNumber}: {insertedCount}/{batch.Count} kayıt eklendi");
        }

        private static List<List<T>> SplitIntoBatches<T>(List<T> list, int batchSize)
        {
            var batches = new List<List<T>>();

            for (int i = 0; i < list.Count; i += batchSize)
            {
                var batch = list.Skip(i).Take(batchSize).ToList();
                batches.Add(batch);
            }

            return batches;
        }

        private static TimeSpan? CalculateEstimatedTimeRemaining(int completedBatches, int totalBatches, TimeSpan elapsed)
        {
            if (completedBatches == 0 || elapsed.TotalSeconds < 1)
                return null;

            var averageTimePerBatch = elapsed.TotalSeconds / completedBatches;
            var remainingBatches = totalBatches - completedBatches;
            var estimatedSecondsRemaining = remainingBatches * averageTimePerBatch;

            return TimeSpan.FromSeconds(estimatedSecondsRemaining);
        }

        protected virtual void OnProgressChanged(MigrationProgressEventArgs e)
        {
            ProgressChanged?.Invoke(this, e);
        }

        #endregion
    }
}