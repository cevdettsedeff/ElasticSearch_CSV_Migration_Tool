using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticSearchPostgreSQLMigrationTool.Models
{
    /// <summary>
    /// Batch validation istatistikleri
    /// </summary>
    public class BatchValidationStats
    {
        public int TotalCount { get; set; }
        public int ValidCount { get; set; }
        public int InvalidCount { get; set; }
        public int WarningCount { get; set; }
        public Dictionary<string, int> MostCommonErrors { get; set; } = new();
        public Dictionary<string, int> MostCommonWarnings { get; set; } = new();

        public double ValidPercentage => TotalCount > 0 ? (double)ValidCount / TotalCount * 100 : 0;
        public double InvalidPercentage => TotalCount > 0 ? (double)InvalidCount / TotalCount * 100 : 0;
        public double WarningPercentage => TotalCount > 0 ? (double)WarningCount / TotalCount * 100 : 0;
    }
}
