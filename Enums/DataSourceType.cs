using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticSearchPostgreSQLMigrationTool.Enums
{
    /// <summary>
    /// Veri kaynağı türlerini tanımlar
    /// </summary>
    public enum DataSourceType
    {
        ElasticSearch,
        CSV,
        Unknown
    }
}
