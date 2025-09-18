using ElasticSearchPostgreSQLMigrationTool.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticSearchPostgreSQLMigrationTool.Interfaces
{
    /// <summary>
    /// ElasticSearch operasyonları için servis interface'i (artık kullanılmayacak)
    /// </summary>
    public interface IElasticSearchService : IDisposable
    {
        /// <summary>
        /// ElasticSearch'den tüm access log kayıtlarını çeker
        /// </summary>
        /// <returns>Access log kayıtlarının koleksiyonu</returns>
        Task<IEnumerable<AccessLog>> GetAllAccessLogsAsync();

        /// <summary>
        /// ElasticSearch bağlantısını test eder
        /// </summary>
        /// <returns>Bağlantı başarılı ise true</returns>
        Task<bool> TestConnectionAsync();

        /// <summary>
        /// Belirtilen index'teki toplam kayıt sayısını döndürür
        /// </summary>
        /// <returns>Toplam kayıt sayısı</returns>
        Task<long> GetTotalRecordCountAsync();

        /// <summary>
        /// ElasticSearch cluster sağlık durumunu kontrol eder
        /// </summary>
        /// <returns>Cluster sağlıklı ise true</returns>
        Task<bool> IsClusterHealthyAsync();
    }
}
