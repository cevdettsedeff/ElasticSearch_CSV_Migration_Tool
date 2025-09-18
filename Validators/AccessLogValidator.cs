using ElasticSearchPostgreSQLMigrationTool.Models;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticSearchPostgreSQLMigrationTool.Validators
{
    /// <summary>
    /// AccessLog model'i için FluentValidation kuralları
    /// </summary>
    public class AccessLogValidator : AbstractValidator<AccessLog>
    {
        public AccessLogValidator()
        {
            // EventName validasyonu
            RuleFor(x => x.EventName)
                .MaximumLength(500)
                .WithMessage("Event name 500 karakterden uzun olamaz");

            // GateName validasyonu
            RuleFor(x => x.GateName)
                .MaximumLength(100)
                .WithMessage("Gate name 100 karakterden uzun olamaz");

            // GksType validasyonu
            RuleFor(x => x.GksType)
                .MaximumLength(50)
                .WithMessage("GKS type 50 karakterden uzun olamaz")
                .Must(BeValidGksType)
                .WithMessage("GKS type geçerli bir değer olmalı (TELPO, HIKVISION, vb.)");

            // Image path validasyonu
            RuleFor(x => x.Image)
                .MaximumLength(1000)
                .WithMessage("Image path 1000 karakterden uzun olamaz")
                .Must(BeValidImagePath)
                .When(x => !string.IsNullOrEmpty(x.Image))
                .WithMessage("Image path geçerli bir format olmalı");

            // IP adresi validasyonu
            RuleFor(x => x.Ip)
                .MaximumLength(45)
                .WithMessage("IP adresi 45 karakterden uzun olamaz")
                .Must(BeValidIpAddress)
                .When(x => !string.IsNullOrEmpty(x.Ip))
                .WithMessage("Geçerli bir IP adresi giriniz");

            // NationalityId validasyonu (Türk kimlik no formatı)
            RuleFor(x => x.NationalityId)
                .MaximumLength(50)
                .WithMessage("Nationality ID 50 karakterden uzun olamaz")
                .Must(BeValidNationalityId)
                .When(x => !string.IsNullOrEmpty(x.NationalityId))
                .WithMessage("Nationality ID geçerli bir format olmalı");

            // PassageDuration validasyonu
            RuleFor(x => x.PassageDuration)
                .GreaterThanOrEqualTo(0)
                .When(x => x.PassageDuration.HasValue)
                .WithMessage("Passage duration negatif olamaz")
                .LessThanOrEqualTo(3600) // Maksimum 1 saat
                .When(x => x.PassageDuration.HasValue)
                .WithMessage("Passage duration 1 saati geçemez");

            // Port validasyonu
            RuleFor(x => x.Port)
                .MaximumLength(10)
                .WithMessage("Port 10 karakterden uzun olamaz")
                .Must(BeValidPort)
                .When(x => !string.IsNullOrEmpty(x.Port))
                .WithMessage("Port geçerli bir değer olmalı (1-65535)");

            // ReaderName validasyonu
            RuleFor(x => x.ReaderName)
                .MaximumLength(100)
                .WithMessage("Reader name 100 karakterden uzun olamaz");

            // Result validasyonu
            RuleFor(x => x.Result)
                .MaximumLength(50)
                .WithMessage("Result 50 karakterden uzun olamaz")
                .Must(BeValidResult)
                .When(x => !string.IsNullOrEmpty(x.Result))
                .WithMessage("Result geçerli bir değer olmalı (PASSED, FAILED, DENIED, vb.)");

            // SerialNumber validasyonu
            RuleFor(x => x.SerialNumber)
                .MaximumLength(100)
                .WithMessage("Serial number 100 karakterden uzun olamaz");

            // EventId validasyonu
            RuleFor(x => x.EventId)
                .GreaterThan(0)
                .When(x => x.EventId.HasValue)
                .WithMessage("Event ID pozitif bir sayı olmalı");

            // StadiumId validasyonu
            RuleFor(x => x.StadiumId)
                .GreaterThan(0)
                .When(x => x.StadiumId.HasValue)
                .WithMessage("Stadium ID pozitif bir sayı olmalı");

            // TransactionId validasyonu
            RuleFor(x => x.TransactionId)
                .GreaterThan(0)
                .When(x => x.TransactionId.HasValue)
                .WithMessage("Transaction ID pozitif bir sayı olmalı");

            // ElasticsearchId validasyonu
            RuleFor(x => x.ElasticsearchId)
                .MaximumLength(100)
                .WithMessage("Elasticsearch ID 100 karakterden uzun olamaz")
                .NotEmpty()
                .WithMessage("Elasticsearch ID boş olamaz");

            // ElasticsearchIndex validasyonu
            RuleFor(x => x.ElasticsearchIndex)
                .MaximumLength(100)
                .WithMessage("Elasticsearch index 100 karakterden uzun olamaz");

            // Tarih validasyonları
            RuleFor(x => x.Timestamp)
                .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
                .When(x => x.Timestamp.HasValue)
                .WithMessage("Timestamp gelecekte olamaz");

            RuleFor(x => x.TransactionTime)
                .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
                .When(x => x.TransactionTime.HasValue)
                .WithMessage("Transaction time gelecekte olamaz");

            // CreatedAt her zaman geçerli olmalı
            RuleFor(x => x.CreatedAt)
                .NotEmpty()
                .WithMessage("Created at boş olamaz");
        }

        /// <summary>
        /// GKS tipinin geçerli olup olmadığını kontrol eder
        /// </summary>
        private static bool BeValidGksType(string? gksType)
        {
            if (string.IsNullOrEmpty(gksType))
                return true;

            var validTypes = new[] { "TELPO", "HIKVISION", "DAHUA", "ZKTECO", "SUPREMA" };
            return validTypes.Contains(gksType.ToUpper());
        }

        /// <summary>
        /// Image path'in geçerli olup olmadığını kontrol eder
        /// </summary>
        private static bool BeValidImagePath(string? imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
                return true;

            // Basit path validasyonu
            var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
            return validExtensions.Any(ext => imagePath.ToLower().EndsWith(ext));
        }

        /// <summary>
        /// IP adresinin geçerli olup olmadığını kontrol eder (IPv4 ve IPv6)
        /// </summary>
        private static bool BeValidIpAddress(string? ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress))
                return true;

            return System.Net.IPAddress.TryParse(ipAddress, out _);
        }

        /// <summary>
        /// Nationality ID'nin geçerli olup olmadığını kontrol eder
        /// </summary>
        private static bool BeValidNationalityId(string? nationalityId)
        {
            if (string.IsNullOrEmpty(nationalityId))
                return true;

            // Türk kimlik no formatı (11 haneli) veya diğer formatlar
            if (nationalityId.Length == 11 && nationalityId.All(char.IsDigit))
            {
                // Türk kimlik no algoritması
                return IsValidTurkishId(nationalityId);
            }

            // Diğer ülke formatları için daha esnek kontrol
            return nationalityId.Length >= 5 && nationalityId.Length <= 50;
        }

        /// <summary>
        /// Türk kimlik numarası algoritması ile validasyon
        /// </summary>
        private static bool IsValidTurkishId(string tcNo)
        {
            if (tcNo.Length != 11 || !tcNo.All(char.IsDigit))
                return false;

            var digits = tcNo.Select(c => int.Parse(c.ToString())).ToArray();

            // İlk rakam 0 olamaz
            if (digits[0] == 0)
                return false;

            // Algoritma kontrolü
            var oddSum = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
            var evenSum = digits[1] + digits[3] + digits[5] + digits[7];

            var checkDigit1 = ((oddSum * 7) - evenSum) % 10;
            var checkDigit2 = (oddSum + evenSum + digits[9]) % 10;

            return digits[9] == checkDigit1 && digits[10] == checkDigit2;
        }

        /// <summary>
        /// Port numarasının geçerli olup olmadığını kontrol eder
        /// </summary>
        private static bool BeValidPort(string? port)
        {
            if (string.IsNullOrEmpty(port))
                return true;

            if (int.TryParse(port, out int portNumber))
            {
                return portNumber >= 1 && portNumber <= 65535;
            }

            return false;
        }

        /// <summary>
        /// Result değerinin geçerli olup olmadığını kontrol eder
        /// </summary>
        private static bool BeValidResult(string? result)
        {
            if (string.IsNullOrEmpty(result))
                return true;

            var validResults = new[] { "PASSED", "FAILED", "DENIED", "ERROR", "TIMEOUT", "BLOCKED" };
            return validResults.Contains(result.ToUpper());
        }
    }
}
