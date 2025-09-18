using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticSearchPostgreSQLMigrationTool.Models
{
    /// <summary>
    /// Configuration bilgileri modeli
    /// </summary>
    public class ConfigurationInfo
    {
        public string Environment { get; set; } = string.Empty;
        public string BasePath { get; set; } = string.Empty;
        public string AppSettingsPath { get; set; } = string.Empty;
        public string EnvironmentAppSettingsPath { get; set; } = string.Empty;
        public bool AppSettingsExists { get; set; }
        public bool EnvironmentAppSettingsExists { get; set; }
        public Dictionary<string, string> ConfigurationSources { get; set; } = new();

        public string GetSummary()
        {
            return $@"
    Configuration Info:
    ==================
    Environment: {Environment}
    Base Path: {BasePath}
    appsettings.json: {(AppSettingsExists ? "✅ Exists" : "❌ Missing")}
    appsettings.{Environment}.json: {(EnvironmentAppSettingsExists ? "✅ Exists" : "❌ Missing")}
    Total Settings: {ConfigurationSources.Count}";
        }
    }
}
