using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace mvc_boilerplate.Models.ViewModels
{
    public class StrongPasswordAttribute : ValidationAttribute
    {
        private static readonly Regex Uppercase = new(@"[A-Z]", RegexOptions.Compiled);
        private static readonly Regex Lowercase = new(@"[a-z]", RegexOptions.Compiled);
        private static readonly Regex Digit = new(@"[0-9]", RegexOptions.Compiled);
        private static readonly Regex NonAlphanumeric = new(@"[^a-zA-Z0-9]", RegexOptions.Compiled);

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var password = value as string;

            // Password is optional on edit — skip validation if left blank
            if (string.IsNullOrEmpty(password))
                return ValidationResult.Success;

            if (password.Length < 8)
                return new ValidationResult("Password must be at least 8 characters long.");

            if (!Uppercase.IsMatch(password))
                return new ValidationResult("Password must contain at least one uppercase letter (A-Z).");

            if (!Lowercase.IsMatch(password))
                return new ValidationResult("Password must contain at least one lowercase letter (a-z).");

            if (!Digit.IsMatch(password))
                return new ValidationResult("Password must contain at least one digit (0-9).");

            if (!NonAlphanumeric.IsMatch(password))
                return new ValidationResult("Password must contain at least one non-alphanumeric character.");

            return ValidationResult.Success;
        }
    }
}