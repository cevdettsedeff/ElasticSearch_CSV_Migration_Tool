using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticSearchPostgreSQLMigrationTool.Interfaces
{
    /// <summary>
    /// Loglama servisi interface'i
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Info seviyesinde log kaydeder
        /// </summary>
        /// <param name="message">Log mesajı</param>
        void LogInfo(string message);

        /// <summary>
        /// Warning seviyesinde log kaydeder
        /// </summary>
        /// <param name="message">Log mesajı</param>
        void LogWarning(string message);

        /// <summary>
        /// Error seviyesinde log kaydeder
        /// </summary>
        /// <param name="message">Log mesajı</param>
        void LogError(string message);

        /// <summary>
        /// Exception ile birlikte error log kaydeder
        /// </summary>
        /// <param name="exception">Exception bilgisi</param>
        /// <param name="message">Log mesajı</param>
        void LogError(Exception exception, string message);

        /// <summary>
        /// Debug seviyesinde log kaydeder
        /// </summary>
        /// <param name="message">Log mesajı</param>
        void LogDebug(string message);

        /// <summary>
        /// Progress bilgisini loglar
        /// </summary>
        /// <param name="current">Mevcut işlem sayısı</param>
        /// <param name="total">Toplam işlem sayısı</param>
        /// <param name="message">Progress mesajı</param>
        void LogProgress(int current, int total, string message);
    }
}
