using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticSearchPostgreSQLMigrationTool.Models
{
    /// <summary>
    /// Validation test sonuçları
    /// </summary>
    public class ValidationTestResult
    {
        public bool AccessLogValidationWorks { get; set; }
        public bool AccessLogValidationCatchesErrors { get; set; }
        public bool MigrationSettingsValidationWorks { get; set; }
        public bool MigrationSettingsValidationCatchesErrors { get; set; }
        public bool AllTestsPassed { get; set; }
        public Exception? TestException { get; set; }
    }
}
