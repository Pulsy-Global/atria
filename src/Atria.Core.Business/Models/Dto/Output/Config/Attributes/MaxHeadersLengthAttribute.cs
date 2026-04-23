using System.ComponentModel.DataAnnotations;

namespace Atria.Core.Business.Models.Dto.Output.Config.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class MaxHeadersLengthAttribute : ValidationAttribute
{
    public int MaxEntries { get; set; } = 50;

    public int MaxKeyLength { get; set; } = 256;

    public int MaxValueLength { get; set; } = 1024;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        if (value is not IDictionary<string, string> headers)
        {
            return new ValidationResult(
                $"{validationContext.DisplayName} must be a string-to-string dictionary.");
        }

        if (headers.Count > MaxEntries)
        {
            return new ValidationResult(
                $"{validationContext.DisplayName} cannot contain more than {MaxEntries} entries.");
        }

        foreach (var (key, val) in headers)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return new ValidationResult($"{validationContext.DisplayName} contains an empty header name.");
            }

            if (key.Length > MaxKeyLength)
            {
                return new ValidationResult(
                    $"{validationContext.DisplayName} header name exceeds {MaxKeyLength} characters.");
            }

            if (val is not null && val.Length > MaxValueLength)
            {
                return new ValidationResult(
                    $"{validationContext.DisplayName} header value exceeds {MaxValueLength} characters.");
            }
        }

        return ValidationResult.Success;
    }
}
