using ElasticSearchPostgreSQLMigrationTool.Interfaces;
using ElasticSearchPostgreSQLMigrationTool.Models;
using Npgsql;
using System.Diagnostics;
using System.Text;

namespace ElasticSearchPostgreSQLMigrationTool.Services
{
    /// <summary>
    /// PostgreSQL operasyonları için servis implementasyonu
    /// </summary>
    public class PostgreSQLService : IPostgreSQLService
    {
        private readonly string _connectionString;
        private readonly ILogger _logger;
        private readonly MigrationSettings _settings;

        public PostgreSQLService(ILogger logger, MigrationSettings settings)
        {
            if (string.IsNullOrEmpty(settings.PostgreConnectionString))
                throw new ArgumentNullException(nameof(settings.PostgreConnectionString), "PostgreSQL connection string boş olamaz.");

            _connectionString = settings.PostgreConnectionString;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings;
        }

        /// <summary>
        /// PostgreSQL bağlantısını test eder
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                _logger.LogDebug("PostgreSQL bağlantısı test ediliyor...");

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new NpgsqlCommand("SELECT version()", connection);
                var version = await command.ExecuteScalarAsync();

                _logger.LogInfo($"PostgreSQL bağlantısı başarılı: {version}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PostgreSQL bağlantı testi başarısız");
                return false;
            }
        }

        /// <summary>
        /// Access logs tablosunun var olup olmadığını kontrol eder
        /// </summary>
        public async Task<bool> TableExistsAsync()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                const string query = @"
                    SELECT EXISTS (
                        SELECT FROM information_schema.tables 
                        WHERE table_schema = 'public' 
                        AND table_name = 'access_logs'
                    );";

                using var command = new NpgsqlCommand(query, connection);
                var result = await command.ExecuteScalarAsync();

                return (bool)result!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tablo varlık kontrolü sırasında hata");
                return false;
            }
        }

        /// <summary>
        /// Access logs tablosunu oluşturur
        /// </summary>
        public async Task CreateTableAsync()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                const string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS access_logs (
                        id SERIAL PRIMARY KEY,
                        access_log_flag BOOLEAN NOT NULL DEFAULT false,
                        area_name TEXT,
                        event_id INTEGER,
                        event_name TEXT,
                        gate_name TEXT,
                        gks_type TEXT,
                        image TEXT,
                        ip TEXT,
                        is_accreditation BOOLEAN NOT NULL DEFAULT false,
                        nationality_id TEXT,
                        passage_duration DECIMAL(10,2),
                        port TEXT,
                        reader_name TEXT,
                        result TEXT,
                        serial_number TEXT,
                        stadium_id INTEGER,
                        timestamp TIMESTAMP WITH TIME ZONE,
                        transaction_id INTEGER,
                        transaction_time TIMESTAMP WITH TIME ZONE,
                        elasticsearch_id TEXT UNIQUE,
                        elasticsearch_index TEXT,
                        elasticsearch_score DECIMAL(10,2),
                        created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
                    );

                    -- Performance indexes
                    CREATE INDEX IF NOT EXISTS idx_access_logs_event_id ON access_logs(event_id);
                    CREATE INDEX IF NOT EXISTS idx_access_logs_transaction_id ON access_logs(transaction_id);
                    CREATE INDEX IF NOT EXISTS idx_access_logs_timestamp ON access_logs(timestamp);
                    CREATE INDEX IF NOT EXISTS idx_access_logs_elasticsearch_id ON access_logs(elasticsearch_id);
                    CREATE INDEX IF NOT EXISTS idx_access_logs_stadium_id ON access_logs(stadium_id);
                    CREATE INDEX IF NOT EXISTS idx_access_logs_result ON access_logs(result);
                    CREATE INDEX IF NOT EXISTS idx_access_logs_gate_name ON access_logs(gate_name);
                    CREATE INDEX IF NOT EXISTS idx_access_logs_reader_name ON access_logs(reader_name);
                    CREATE INDEX IF NOT EXISTS idx_access_logs_created_at ON access_logs(created_at);

                    -- Composite indexes for common queries
                    CREATE INDEX IF NOT EXISTS idx_access_logs_event_stadium ON access_logs(event_id, stadium_id);
                    CREATE INDEX IF NOT EXISTS idx_access_logs_timestamp_result ON access_logs(timestamp, result);
                ";

                using var command = new NpgsqlCommand(createTableQuery, connection);
                await command.ExecuteNonQueryAsync();

                _logger.LogInfo("PostgreSQL access_logs tablosu ve index'ler başarıyla oluşturuldu");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tablo oluşturma sırasında hata");
                throw;
            }
        }

        /// <summary>
        /// Tablodaki kayıt sayısını döndürür
        /// </summary>
        public async Task<long> GetRecordCountAsync()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                const string query = "SELECT COUNT(*) FROM access_logs;";
                using var command = new NpgsqlCommand(query, connection);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt64(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kayıt sayısı alınırken hata");
                return 0;
            }
        }

        /// <summary>
        /// Tabloyu temizler
        /// </summary>
        public async Task TruncateTableAsync()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                const string query = "TRUNCATE TABLE access_logs RESTART IDENTITY;";
                using var command = new NpgsqlCommand(query, connection);
                await command.ExecuteNonQueryAsync();

                _logger.LogInfo("access_logs tablosu temizlendi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tablo temizleme sırasında hata");
                throw;
            }
        }

        /// <summary>
        /// Duplicate kayıtları temizler
        /// </summary>
        public async Task<int> CleanupDuplicatesAsync()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                const string query = @"
                    DELETE FROM access_logs a
                    USING access_logs b 
                    WHERE a.id < b.id 
                    AND a.elasticsearch_id = b.elasticsearch_id 
                    AND a.elasticsearch_id IS NOT NULL;";

                using var command = new NpgsqlCommand(query, connection);
                var deletedCount = await command.ExecuteNonQueryAsync();

                _logger.LogInfo($"{deletedCount} duplicate kayıt temizlendi");
                return deletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Duplicate temizleme sırasında hata");
                return 0;
            }
        }

        /// <summary>
        /// Batch halinde kayıtları PostgreSQL'e ekler
        /// </summary>
        public async Task<int> InsertBatchAsync(IEnumerable<AccessLog> logs)
        {
            var logList = logs.ToList();
            if (!logList.Any())
                return 0;

            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                int insertedCount = 0;

                try
                {
                    // Bulk insert için COPY kullan (çok daha hızlı)
                    if (_settings.BatchSize > 100)
                    {
                        insertedCount = await BulkInsertWithCopyAsync(connection, transaction, logList);
                    }
                    else
                    {
                        insertedCount = await InsertWithParametersAsync(connection, transaction, logList);
                    }

                    await transaction.CommitAsync();

                    stopwatch.Stop();
                    _logger.LogDebug($"Batch ({logList.Count} kayıt) {stopwatch.ElapsedMilliseconds}ms'de tamamlandı");

                    return insertedCount;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, $"Batch insert sırasında hata ({logList.Count} kayıt)");
                throw;
            }
        }

        /// <summary>
        /// COPY komutu ile bulk insert (çok hızlı)
        /// </summary>
        private async Task<int> BulkInsertWithCopyAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, List<AccessLog> logs)
        {
            const string copyCommand = @"
                COPY access_logs (
                    access_log_flag, area_name, event_id, event_name, gate_name, gks_type,
                    image, ip, is_accreditation, nationality_id, passage_duration,
                    port, reader_name, result, serial_number, stadium_id,
                    timestamp, transaction_id, transaction_time, elasticsearch_id,
                    elasticsearch_index, elasticsearch_score, created_at
                ) FROM STDIN (FORMAT BINARY)";

            using var writer = await connection.BeginBinaryImportAsync(copyCommand);

            int insertedCount = 0;
            foreach (var log in logs)
            {
                try
                {
                    await writer.StartRowAsync();
                    await writer.WriteAsync(log.AccessLogFlag);
                    await writer.WriteAsync(log.AreaName, NpgsqlTypes.NpgsqlDbType.Text);
                    await writer.WriteAsync(log.EventId, NpgsqlTypes.NpgsqlDbType.Integer);
                    await writer.WriteAsync(log.EventName, NpgsqlTypes.NpgsqlDbType.Text);
                    await writer.WriteAsync(log.GateName, NpgsqlTypes.NpgsqlDbType.Text);
                    await writer.WriteAsync(log.GksType, NpgsqlTypes.NpgsqlDbType.Text);
                    await writer.WriteAsync(log.Image, NpgsqlTypes.NpgsqlDbType.Text);
                    await writer.WriteAsync(log.Ip, NpgsqlTypes.NpgsqlDbType.Text);
                    await writer.WriteAsync(log.IsAccreditation);
                    await writer.WriteAsync(log.NationalityId, NpgsqlTypes.NpgsqlDbType.Text);
                    await writer.WriteAsync(log.PassageDuration, NpgsqlTypes.NpgsqlDbType.Numeric);
                    await writer.WriteAsync(log.Port, NpgsqlTypes.NpgsqlDbType.Text);
                    await writer.WriteAsync(log.ReaderName, NpgsqlTypes.NpgsqlDbType.Text);
                    await writer.WriteAsync(log.Result, NpgsqlTypes.NpgsqlDbType.Text);
                    await writer.WriteAsync(log.SerialNumber, NpgsqlTypes.NpgsqlDbType.Text);
                    await writer.WriteAsync(log.StadiumId, NpgsqlTypes.NpgsqlDbType.Integer);
                    await writer.WriteAsync(log.Timestamp, NpgsqlTypes.NpgsqlDbType.TimestampTz);
                    await writer.WriteAsync(log.TransactionId, NpgsqlTypes.NpgsqlDbType.Integer);
                    await writer.WriteAsync(log.TransactionTime, NpgsqlTypes.NpgsqlDbType.TimestampTz);
                    await writer.WriteAsync(log.ElasticsearchId, NpgsqlTypes.NpgsqlDbType.Text);
                    await writer.WriteAsync(log.ElasticsearchIndex, NpgsqlTypes.NpgsqlDbType.Text);
                    await writer.WriteAsync(log.ElasticsearchScore, NpgsqlTypes.NpgsqlDbType.Numeric);
                    await writer.WriteAsync(log.CreatedAt, NpgsqlTypes.NpgsqlDbType.TimestampTz);

                    insertedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Kayıt insert edilemedi: {ex.Message}");
                }
            }

            await writer.CompleteAsync();
            return insertedCount;
        }

        /// <summary>
        /// Parametreli insert (küçük batch'ler için)
        /// </summary>
        private async Task<int> InsertWithParametersAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, List<AccessLog> logs)
        {
            const string insertQuery = @"
                INSERT INTO access_logs (
                    access_log_flag, area_name, event_id, event_name, gate_name, gks_type,
                    image, ip, is_accreditation, nationality_id, passage_duration,
                    port, reader_name, result, serial_number, stadium_id,
                    timestamp, transaction_id, transaction_time, elasticsearch_id,
                    elasticsearch_index, elasticsearch_score, created_at
                ) VALUES (
                    @access_log_flag, @area_name, @event_id, @event_name, @gate_name, @gks_type,
                    @image, @ip, @is_accreditation, @nationality_id, @passage_duration,
                    @port, @reader_name, @result, @serial_number, @stadium_id,
                    @timestamp, @transaction_id, @transaction_time, @elasticsearch_id,
                    @elasticsearch_index, @elasticsearch_score, @created_at
                )
                ON CONFLICT (elasticsearch_id) DO NOTHING";

            int insertedCount = 0;

            foreach (var log in logs)
            {
                using var command = new NpgsqlCommand(insertQuery, connection, transaction);

                AddParameters(command, log);

                try
                {
                    var result = await command.ExecuteNonQueryAsync();
                    insertedCount += result;
                }
                catch (Exception ex)
                {
                    if (_settings.StopOnError)
                        throw;

                    _logger.LogWarning($"Kayıt insert edilemedi: {ex.Message}");
                }
            }

            return insertedCount;
        }

        /// <summary>
        /// Command'e parametreleri ekler
        /// </summary>
        private static void AddParameters(NpgsqlCommand command, AccessLog log)
        {
            command.Parameters.AddWithValue("@access_log_flag", log.AccessLogFlag);
            command.Parameters.AddWithValue("@area_name", log.AreaName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@event_id", log.EventId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@event_name", log.EventName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@gate_name", log.GateName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@gks_type", log.GksType ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@image", log.Image ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ip", log.Ip ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@is_accreditation", log.IsAccreditation);
            command.Parameters.AddWithValue("@nationality_id", log.NationalityId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@passage_duration", log.PassageDuration ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@port", log.Port ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@reader_name", log.ReaderName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@result", log.Result ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@serial_number", log.SerialNumber ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@stadium_id", log.StadiumId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@timestamp", log.Timestamp ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@transaction_id", log.TransactionId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@transaction_time", log.TransactionTime ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@elasticsearch_id", log.ElasticsearchId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@elasticsearch_index", log.ElasticsearchIndex ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@elasticsearch_score", log.ElasticsearchScore ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@created_at", log.CreatedAt);
        }
    }
}