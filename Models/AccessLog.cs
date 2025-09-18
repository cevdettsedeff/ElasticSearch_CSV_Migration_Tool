using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticSearchPostgreSQLMigrationTool.Models
{
    /// <summary>
    /// ElasticSearch'den PostgreSQL'e aktarılacak access log modelini temsil eder
    /// </summary>
    public class AccessLog
    {
        /// <summary>
        /// PostgreSQL'deki otomatik artan primary key
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Access log flag - ElasticSearch'deki accessLog alanından gelir
        /// </summary>
        public bool AccessLogFlag { get; set; }

        /// <summary>
        /// Alan adı
        /// </summary>
        public string? AreaName { get; set; }

        /// <summary>
        /// Etkinlik ID'si
        /// </summary>
        public int? EventId { get; set; }

        /// <summary>
        /// Etkinlik adı
        /// </summary>
        public string? EventName { get; set; }

        /// <summary>
        /// Kapı adı
        /// </summary>
        public string? GateName { get; set; }

        /// <summary>
        /// GKS tipi (TELPO, vb.)
        /// </summary>
        public string? GksType { get; set; }

        /// <summary>
        /// Resim yolu
        /// </summary>
        public string? Image { get; set; }

        /// <summary>
        /// IP adresi
        /// </summary>
        public string? Ip { get; set; }

        /// <summary>
        /// Akreditasyon durumu
        /// </summary>
        public bool IsAccreditation { get; set; }

        /// <summary>
        /// Kimlik numarası
        /// </summary>
        public string? NationalityId { get; set; }

        /// <summary>
        /// Geçiş süresi (saniye)
        /// </summary>
        public decimal? PassageDuration { get; set; }

        /// <summary>
        /// Port bilgisi
        /// </summary>
        public string? Port { get; set; }

        /// <summary>
        /// Okuyucu adı
        /// </summary>
        public string? ReaderName { get; set; }

        /// <summary>
        /// Geçiş sonucu (PASSED, FAILED, vb.)
        /// </summary>
        public string? Result { get; set; }

        /// <summary>
        /// Seri numarası
        /// </summary>
        public string? SerialNumber { get; set; }

        /// <summary>
        /// Stadyum ID'si
        /// </summary>
        public int? StadiumId { get; set; }

        /// <summary>
        /// ElasticSearch'deki timestamp
        /// </summary>
        public DateTime? Timestamp { get; set; }

        /// <summary>
        /// Transaction ID'si
        /// </summary>
        public int? TransactionId { get; set; }

        /// <summary>
        /// Transaction zamanı
        /// </summary>
        public DateTime? TransactionTime { get; set; }

        /// <summary>
        /// ElasticSearch doküman ID'si
        /// </summary>
        public string? ElasticsearchId { get; set; }

        /// <summary>
        /// ElasticSearch index adı
        /// </summary>
        public string? ElasticsearchIndex { get; set; }

        /// <summary>
        /// ElasticSearch score değeri
        /// </summary>
        public decimal? ElasticsearchScore { get; set; }

        /// <summary>
        /// PostgreSQL'e eklenme zamanı
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
