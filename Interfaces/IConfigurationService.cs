using ElasticSearchPostgreSQLMigrationTool.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticSearchPostgreSQLMigrationTool.Interfaces
{

        /// <summary>
        /// Yapılandırma servisi interface'i
        /// </summary>
        public interface IConfigurationService
        {
            /// <summary>
            /// Migration ayarlarını yükler
            /// </summary>
            /// <returns>Migration ayarları</returns>
            MigrationSettings GetSettings();

            /// <summary>
            /// Ayarları doğrular
            /// </summary>
            void ValidateSettings();

            /// <summary>
            /// Ayarları dosyadan yükler
            /// </summary>
            /// <param name="filePath">Ayar dosyası yolu</param>
            /// <returns>Migration ayarları</returns>
            Task<MigrationSettings> LoadFromFileAsync(string filePath);

            /// <summary>
            /// Ayarları dosyaya kaydeder
            /// </summary>
            /// <param name="settings">Kaydedilecek ayarlar</param>
            /// <param name="filePath">Hedef dosya yolu</param>
            Task SaveToFileAsync(MigrationSettings settings, string filePath);
        }
    }

