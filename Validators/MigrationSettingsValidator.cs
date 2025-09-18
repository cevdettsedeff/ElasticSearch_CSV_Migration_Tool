using ElasticSearchPostgreSQLMigrationTool.Models;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ElasticSearchPostgreSQLMigrationTool.Validators
{

    /// <summary>
    /// MigrationSettings model'i için FluentValidation kuralları
    /// </summary>
    public class MigrationSettingsValidator : AbstractValidator<MigrationSettings>
    {
        public MigrationSettingsValidator()
        {
            // PostgreSQL Connection String validasyonu
            RuleFor(x => x.PostgreConnectionString)
                .NotEmpty()
                .WithMessage("PostgreSQL connection string boş olamaz")
                .Must(BeValidConnectionString)
                .WithMessage("PostgreSQL connection string geçerli format olmalı");

            // ElasticSearch Nodes validasyonu
            RuleFor(x => x.ElasticSearchNodes)
                .NotNull()
                .WithMessage("ElasticSearch nodes boş olamaz")
                .Must(x => x != null && x.Length > 0)
                .WithMessage("En az bir ElasticSearch node belirtilmeli");

            RuleForEach(x => x.ElasticSearchNodes)
                .Must(BeValidUrl)
                .WithMessage("ElasticSearch node'ları geçerli URL formatında olmalı");

            // Index Name validasyonu
            RuleFor(x => x.IndexName)
                .NotEmpty()
                .WithMessage("Index name boş olamaz")
                .Must(BeValidIndexName)
                .WithMessage("Index name geçerli ElasticSearch index formatında olmalı");

            // Batch Size validasyonu
            RuleFor(x => x.BatchSize)
                .GreaterThan(0)
                .WithMessage("Batch size 0'dan büyük olmalı")
                .LessThanOrEqualTo(10000)
                .WithMessage("Batch size 10,000'den küçük olmalı (performans için)");

            // Scroll Timeout validasyonu
            RuleFor(x => x.ScrollTimeout)
                .NotEmpty()
                .WithMessage("Scroll timeout boş olamaz")
                .Must(BeValidTimeSpan)
                .WithMessage("Scroll timeout geçerli format olmalı (örn: 5m, 1h)");

            // Request Timeout validasyonu
            RuleFor(x => x.RequestTimeoutMinutes)
                .GreaterThan(0)
                .WithMessage("Request timeout 0'dan büyük olmalı")
                .LessThanOrEqualTo(60)
                .WithMessage("Request timeout 60 dakikadan küçük olmalı");

            // Max Degree of Parallelism validasyonu
            RuleFor(x => x.MaxDegreeOfParallelism)
                .GreaterThan(0)
                .WithMessage("Max degree of parallelism 0'dan büyük olmalı")
                .LessThanOrEqualTo(Environment.ProcessorCount * 2)
                .WithMessage($"Max degree of parallelism {Environment.ProcessorCount * 2}'den küçük olmalı");

            // Paralel processing etkinse, batch size kontrolü
            RuleFor(x => x.BatchSize)
                .LessThanOrEqualTo(1000)
                .When(x => x.EnableParallelProcessing)
                .WithMessage("Paralel processing etkinken batch size 1000'den küçük olmalı");

            // LogLevel validasyonu
            RuleFor(x => x.LogLevel)
                .IsInEnum()
                .WithMessage("Log level geçerli bir değer olmalı");
        }

        /// <summary>
        /// PostgreSQL connection string'inin geçerli olup olmadığını kontrol eder
        /// </summary>
        private static bool BeValidConnectionString(string? connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return false;

            // Temel PostgreSQL connection string kontrolü
            var requiredParts = new[] { "Host", "Database", "Username" };
            return requiredParts.All(part =>
                connectionString.Contains($"{part}=", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// URL'in geçerli olup olmadığını kontrol eder
        /// </summary>
        private static bool BeValidUrl(string? url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            return Uri.TryCreate(url, UriKind.Absolute, out Uri? result) &&
                   (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// ElasticSearch index name'inin geçerli olup olmadığını kontrol eder
        /// </summary>
        private static bool BeValidIndexName(string? indexName)
        {
            if (string.IsNullOrEmpty(indexName))
                return false;

            // ElasticSearch index naming rules
            // - Lowercase only
            // - Cannot start with -, _, +
            // - Cannot be . or ..
            // - Cannot contain \, /, *, ?, ", <, >, |, space, comma, #
            // - Cannot contain uppercase letters
            // - Cannot be longer than 255 bytes

            if (indexName.Length > 255)
                return false;

            if (indexName == "." || indexName == "..")
                return false;

            if (indexName.StartsWith("-") || indexName.StartsWith("_") || indexName.StartsWith("+"))
                return false;

            var invalidChars = new[] { '\\', '/', '*', '?', '"', '<', '>', '|', ' ', ',', '#' };
            if (indexName.Any(c => invalidChars.Contains(c)))
                return false;

            if (indexName.Any(char.IsUpper))
                return false;

            return true;
        }

        /// <summary>
        /// TimeSpan formatının geçerli olup olmadığını kontrol eder
        /// </summary>
        private static bool BeValidTimeSpan(string? timeSpan)
        {
            if (string.IsNullOrEmpty(timeSpan))
                return false;

            // ElasticSearch time units: d, h, m, s, ms, micros, nanos
            var pattern = @"^(\d+)(d|h|m|s|ms|micros|nanos)$";
            return Regex.IsMatch(timeSpan, pattern, RegexOptions.IgnoreCase);
        }
    }
}
