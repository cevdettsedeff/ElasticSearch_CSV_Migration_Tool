using ElasticSearchPostgreSQLMigrationTool.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticSearchPostgreSQLMigrationTool.Interfaces
{
    /// <summary>
    /// PostgreSQL operasyonları için servis interface'i
    /// </summary>
    public interface IPostgreSQLService
    {
        /// <summary>
        /// Access logs tablosunu oluşturur
        /// </summary>
        Task CreateTableAsync();

        /// <summary>
        /// Batch halinde kayıtları PostgreSQL'e ekler
        /// </summary>
        /// <param name="logs">Eklenecek access log kayıtları</param>
        /// <returns>Başarıyla eklenen kayıt sayısı</returns>
        Task<int> InsertBatchAsync(IEnumerable<AccessLog> logs);

        /// <summary>
        /// PostgreSQL bağlantısını test eder
        /// </summary>
        /// <returns>Bağlantı başarılı ise true</returns>
        Task<bool> TestConnectionAsync();

        /// <summary>
        /// Access logs tablosunun var olup olmadığını kontrol eder
        /// </summary>
        /// <returns>Tablo varsa true</returns>
        Task<bool> TableExistsAsync();

        /// <summary>
        /// Access logs tablosundaki toplam kayıt sayısını döndürür
        /// </summary>
        /// <returns>Tablodaki kayıt sayısı</returns>
        Task<long> GetRecordCountAsync();

        /// <summary>
        /// Access logs tablosunu temizler (TRUNCATE)
        /// </summary>
        Task TruncateTableAsync();

        /// <summary>
        /// Duplicate kayıtları temizler
        /// </summary>
        /// <returns>Silinen duplicate kayıt sayısı</returns>
        Task<int> CleanupDuplicatesAsync();
    }
}
