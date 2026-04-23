using System.ComponentModel.DataAnnotations;

namespace Atria.Core.Business.Models.Dto.Output.Config;

public abstract class ConfigBaseDto : IValidatableObject
{
    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        foreach (var property in GetType().GetProperties())
        {
            var value = property.GetValue(this);
            var context = new ValidationContext(this) { MemberName = property.Name };
            Validator.TryValidateProperty(value, context, results);
        }

        return results;
    }
}
