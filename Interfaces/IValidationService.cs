using ElasticSearchPostgreSQLMigrationTool.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticSearchPostgreSQLMigrationTool.Interfaces
{
    /// <summary>
    /// Validation servisi interface'i
    /// </summary>
    public interface IValidationService
    {
        /// <summary>
        /// AccessLog modelini doğrular
        /// </summary>
        /// <param name="accessLog">Doğrulanacak model</param>
        /// <returns>Doğrulama sonucu</returns>
        ValidationResult ValidateAccessLog(AccessLog accessLog);

        /// <summary>
        /// MigrationSettings modelini doğrular
        /// </summary>
        /// <param name="settings">Doğrulanacak ayarlar</param>
        /// <returns>Doğrulama sonucu</returns>
        ValidationResult ValidateMigrationSettings(MigrationSettings settings);

        /// <summary>
        /// Batch halindeki kayıtları doğrular
        /// </summary>
        /// <param name="logs">Doğrulanacak kayıtlar</param>
        /// <returns>Doğrulama sonuçları</returns>
        IEnumerable<ValidationResult> ValidateBatch(IEnumerable<AccessLog> logs);
    }
}
