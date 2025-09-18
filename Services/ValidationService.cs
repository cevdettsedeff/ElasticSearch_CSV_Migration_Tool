using ElasticSearchPostgreSQLMigrationTool.Enums;
using ElasticSearchPostgreSQLMigrationTool.Interfaces;
using ElasticSearchPostgreSQLMigrationTool.Models;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticSearchPostgreSQLMigrationTool.Services
{
        /// <summary>
        /// Validation servisi implementasyonu
        /// </summary>
        public class ValidationService : IValidationService
        {
            private readonly IValidator<AccessLog> _accessLogValidator;
            private readonly IValidator<MigrationSettings> _migrationSettingsValidator;
            private readonly ILogger _logger;

            public ValidationService(
                IValidator<AccessLog> accessLogValidator,
                IValidator<MigrationSettings> migrationSettingsValidator,
                ILogger logger)
            {
                _accessLogValidator = accessLogValidator ?? throw new ArgumentNullException(nameof(accessLogValidator));
                _migrationSettingsValidator = migrationSettingsValidator ?? throw new ArgumentNullException(nameof(migrationSettingsValidator));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            /// <summary>
            /// AccessLog modelini doğrular
            /// </summary>
            public ValidationResult ValidateAccessLog(AccessLog accessLog)
            {
                if (accessLog == null)
                {
                    return ValidationResult.Failure("AccessLog null olamaz");
                }

                try
                {
                    var fluentResult = _accessLogValidator.Validate(accessLog);

                    if (fluentResult.IsValid)
                    {
                        return ValidationResult.Success(accessLog);
                    }

                    var errors = fluentResult.Errors.Select(e => e.ErrorMessage).ToArray();
                    return ValidationResult.Failure(errors);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AccessLog validation sırasında hata");
                    return ValidationResult.Failure($"Validation hatası: {ex.Message}");
                }
            }

            /// <summary>
            /// MigrationSettings modelini doğrular
            /// </summary>
            public ValidationResult ValidateMigrationSettings(MigrationSettings settings)
            {
                if (settings == null)
                {
                    return ValidationResult.Failure("MigrationSettings null olamaz");
                }

                try
                {
                    var fluentResult = _migrationSettingsValidator.Validate(settings);

                    if (fluentResult.IsValid)
                    {
                        return ValidationResult.Success(settings);
                    }

                    var errors = fluentResult.Errors.Select(e => e.ErrorMessage).ToArray();
                    return ValidationResult.Failure(errors);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MigrationSettings validation sırasında hata");
                    return ValidationResult.Failure($"Validation hatası: {ex.Message}");
                }
            }

            /// <summary>
            /// Batch halindeki kayıtları doğrular
            /// </summary>
            public IEnumerable<ValidationResult> ValidateBatch(IEnumerable<AccessLog> logs)
            {
                if (logs == null)
                {
                    yield return ValidationResult.Failure("Logs koleksiyonu null olamaz");
                    yield break;
                }

                var logList = logs.ToList();

                if (!logList.Any())
                {
                    yield return ValidationResult.Failure("Logs koleksiyonu boş olamaz");
                    yield break;
                }

                _logger.LogDebug($"Batch validation başlıyor: {logList.Count} kayıt");

                var validCount = 0;
                var invalidCount = 0;
                var warningCount = 0;

                foreach (var log in logList)
                {
                    var result = ValidateAccessLogWithWarnings(log);

                    if (result.IsValid)
                    {
                        validCount++;
                        if (result.WarningMessages.Any())
                        {
                            warningCount++;
                        }
                    }
                    else
                    {
                        invalidCount++;
                    }

                    yield return result;
                }

                _logger.LogDebug($"Batch validation tamamlandı: {validCount} geçerli, {invalidCount} geçersiz, {warningCount} uyarılı");
            }

            /// <summary>
            /// AccessLog'u uyarılarla birlikte doğrular
            /// </summary>
            private ValidationResult ValidateAccessLogWithWarnings(AccessLog accessLog)
            {
                var result = ValidateAccessLog(accessLog);

                if (!result.IsValid)
                    return result;

                // Uyarı kontrollerini yap
                var warnings = new List<string>();

                // Eksik veri uyarıları
                if (string.IsNullOrEmpty(accessLog.EventName))
                    warnings.Add("Event name boş");

                if (string.IsNullOrEmpty(accessLog.GateName))
                    warnings.Add("Gate name boş");

                if (string.IsNullOrEmpty(accessLog.ReaderName))
                    warnings.Add("Reader name boş");

                if (!accessLog.EventId.HasValue)
                    warnings.Add("Event ID boş");

                if (!accessLog.StadiumId.HasValue)
                    warnings.Add("Stadium ID boş");

                if (!accessLog.TransactionId.HasValue)
                    warnings.Add("Transaction ID boş");

                if (!accessLog.Timestamp.HasValue)
                    warnings.Add("Timestamp boş");

                // Şüpheli veri uyarıları
                if (accessLog.PassageDuration.HasValue && accessLog.PassageDuration > 300)
                    warnings.Add($"Passage duration çok yüksek: {accessLog.PassageDuration}s");

                if (accessLog.TransactionTime.HasValue && accessLog.Timestamp.HasValue)
                {
                    var timeDiff = Math.Abs((accessLog.TransactionTime.Value - accessLog.Timestamp.Value).TotalMinutes);
                    if (timeDiff > 60)
                        warnings.Add($"Transaction time ile timestamp arasında büyük fark: {timeDiff:F1} dakika");
                }

                // IP format kontrolü
                if (!string.IsNullOrEmpty(accessLog.Ip) && !IsValidIpFormat(accessLog.Ip))
                    warnings.Add($"Şüpheli IP formatı: {accessLog.Ip}");

                // Port kontrolü
                if (!string.IsNullOrEmpty(accessLog.Port) && int.TryParse(accessLog.Port, out int port))
                {
                    if (port < 1024 || port > 65535)
                        warnings.Add($"Şüpheli port numarası: {port}");
                }

                if (warnings.Any())
                {
                    return ValidationResult.SuccessWithWarnings(accessLog, warnings.ToArray());
                }

                return result;
            }

            /// <summary>
            /// IP formatının geçerli olup olmadığını kontrol eder
            /// </summary>
            private static bool IsValidIpFormat(string ip)
            {
                return System.Net.IPAddress.TryParse(ip, out _);
            }

            /// <summary>
            /// Batch validation istatistiklerini döndürür
            /// </summary>
            public BatchValidationStats GetBatchValidationStats(IEnumerable<ValidationResult> results)
            {
                var resultList = results.ToList();

                return new BatchValidationStats
                {
                    TotalCount = resultList.Count,
                    ValidCount = resultList.Count(r => r.IsValid),
                    InvalidCount = resultList.Count(r => !r.IsValid),
                    WarningCount = resultList.Count(r => r.WarningMessages.Any()),
                    MostCommonErrors = GetMostCommonMessages(resultList.SelectMany(r => r.ErrorMessages)),
                    MostCommonWarnings = GetMostCommonMessages(resultList.SelectMany(r => r.WarningMessages))
                };
            }

            /// <summary>
            /// En sık görülen mesajları döndürür
            /// </summary>
            private static Dictionary<string, int> GetMostCommonMessages(IEnumerable<string> messages)
            {
                return messages
                    .GroupBy(m => m)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count());
            }

            /// <summary>
            /// Validation kurallarını test eder
            /// </summary>
            public ValidationTestResult TestValidationRules()
            {
                var testResult = new ValidationTestResult();

                try
                {
                    // AccessLog validation test
                    var validAccessLog = CreateValidAccessLog();
                    var accessLogResult = ValidateAccessLog(validAccessLog);
                    testResult.AccessLogValidationWorks = accessLogResult.IsValid;

                    var invalidAccessLog = CreateInvalidAccessLog();
                    var invalidAccessLogResult = ValidateAccessLog(invalidAccessLog);
                    testResult.AccessLogValidationCatchesErrors = !invalidAccessLogResult.IsValid;

                    // MigrationSettings validation test
                    var validSettings = CreateValidMigrationSettings();
                    var settingsResult = ValidateMigrationSettings(validSettings);
                    testResult.MigrationSettingsValidationWorks = settingsResult.IsValid;

                    var invalidSettings = CreateInvalidMigrationSettings();
                    var invalidSettingsResult = ValidateMigrationSettings(invalidSettings);
                    testResult.MigrationSettingsValidationCatchesErrors = !invalidSettingsResult.IsValid;

                    testResult.AllTestsPassed = testResult.AccessLogValidationWorks &&
                                              testResult.AccessLogValidationCatchesErrors &&
                                              testResult.MigrationSettingsValidationWorks &&
                                              testResult.MigrationSettingsValidationCatchesErrors;
                }
                catch (Exception ex)
                {
                    testResult.TestException = ex;
                    _logger.LogError(ex, "Validation test sırasında hata");
                }

                return testResult;
            }

            private static AccessLog CreateValidAccessLog()
            {
                return new AccessLog
                {
                    AccessLogFlag = true,
                    EventId = 1,
                    EventName = "Test Event",
                    GateName = "G1",
                    GksType = "TELPO",
                    Ip = "192.168.1.1",
                    IsAccreditation = false,
                    NationalityId = "12345678901",
                    PassageDuration = 3.5m,
                    Port = "6666",
                    ReaderName = "G1-1",
                    Result = "PASSED",
                    SerialNumber = "ABC123",
                    StadiumId = 1,
                    TransactionId = 1001,
                    Timestamp = DateTime.UtcNow.AddMinutes(-1),
                    TransactionTime = DateTime.UtcNow.AddMinutes(-1),
                    ElasticsearchId = "test-id-123"
                };
            }

            private static AccessLog CreateInvalidAccessLog()
            {
                return new AccessLog
                {
                    // EventName çok uzun
                    EventName = new string('A', 600),
                    // Geçersiz IP
                    Ip = "999.999.999.999",
                    // Negatif passage duration
                    PassageDuration = -10,
                    // Geçersiz port
                    Port = "99999",
                    // Geçersiz result
                    Result = "INVALID_RESULT"
                };
            }

            private static MigrationSettings CreateValidMigrationSettings()
            {
                return new MigrationSettings
                {
                    PostgreConnectionString = "Host=localhost;Database=test;Username=user;Password=pass;",
                    BatchSize = 1000,
                    DryRun = false,
                    TruncateBeforeMigration = false,
                    EnableParallelProcessing = false,
                    MaxDegreeOfParallelism = 2,
                    StopOnError = true,
                    IgnoreDuplicates = true,
                    LogLevel = LogLevel.Info
                };
            }

            private static MigrationSettings CreateInvalidMigrationSettings()
            {
                return new MigrationSettings
                {
                    // Eksik connection string
                    PostgreConnectionString = "",
                    // Negatif batch size
                    BatchSize = -10,
                    // Geçersiz MaxDegreeOfParallelism
                    MaxDegreeOfParallelism = 0
                };
            }
        }
    }
