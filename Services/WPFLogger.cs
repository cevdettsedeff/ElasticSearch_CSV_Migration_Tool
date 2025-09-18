using ElasticSearchPostgreSQLMigrationTool.ViewModels;
using ElasticSearchPostgreSQLMigrationTool.Interfaces;
using System;

namespace ElasticSearchPostgreSQLMigrationTool.Services
{
    /// <summary>
    /// WPF için özel logger implementasyonu
    /// </summary>
    public class WPFLogger : ILogger
    {
        private readonly MainViewModel _viewModel;

        public WPFLogger(MainViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }

        public void LogDebug(string message)
        {
            _viewModel.AddLog($"🔍 {message}");
        }

        public void LogInfo(string message)
        {
            _viewModel.AddLog(message);
        }

        public void LogWarning(string message)
        {
            _viewModel.AddLog($"⚠️ {message}");
        }

        public void LogError(Exception exception, string message)
        {
            _viewModel.AddLog($"❌ {message}: {exception.Message}");
        }

        public void LogError(string message)
        {
            _viewModel.AddLog($"❌ {message}");
        }

        public void LogProgress(int current, int total, string message)
        {
            var percentage = total > 0 ? (double)current / total * 100 : 0;
            _viewModel.AddLog($"📈 {message} ({percentage:F1}%)");
        }
    }
}