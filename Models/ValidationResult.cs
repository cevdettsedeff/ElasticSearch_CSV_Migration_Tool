using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticSearchPostgreSQLMigrationTool.Models
{
    /// <summary>
    /// Validation sonucu modeli
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Doğrulama başarılı mı
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Doğrulama hata mesajları
        /// </summary>
        public List<string> ErrorMessages { get; set; } = new();

        /// <summary>
        /// Doğrulama uyarı mesajları
        /// </summary>
        public List<string> WarningMessages { get; set; } = new();

        /// <summary>
        /// Doğrulanmış nesne referansı
        /// </summary>
        public object? ValidatedObject { get; set; }

        /// <summary>
        /// Başarılı validation result oluşturur
        /// </summary>
        public static ValidationResult Success(object validatedObject)
        {
            return new ValidationResult
            {
                IsValid = true,
                ValidatedObject = validatedObject
            };
        }

        /// <summary>
        /// Başarısız validation result oluşturur
        /// </summary>
        public static ValidationResult Failure(params string[] errors)
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessages = errors.ToList()
            };
        }

        /// <summary>
        /// Uyarılı ama başarılı validation result oluşturur
        /// </summary>
        public static ValidationResult SuccessWithWarnings(object validatedObject, params string[] warnings)
        {
            return new ValidationResult
            {
                IsValid = true,
                ValidatedObject = validatedObject,
                WarningMessages = warnings.ToList()
            };
        }
    }
}