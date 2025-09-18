using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticSearchPostgreSQLMigrationTool.Enums
{
    public enum LogLevel
    {
        /// <summary>
        /// Sadece kritik hatalar
        /// </summary>
        Error = 0,

        /// <summary>
        /// Uyarılar ve hatalar
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Genel bilgiler, uyarılar ve hatalar
        /// </summary>
        Info = 2,

        /// <summary>
        /// Detaylı debug bilgileri
        /// </summary>
        Debug = 3
    }
}
