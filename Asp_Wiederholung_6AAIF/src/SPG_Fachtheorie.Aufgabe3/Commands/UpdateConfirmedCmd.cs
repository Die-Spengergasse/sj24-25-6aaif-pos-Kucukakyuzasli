using System.ComponentModel.DataAnnotations;

namespace SPG_Fachtheorie.Aufgabe3.Commands
{
    public record UpdateConfirmedCmd(
        int Id,
        DateTime? Confirmed
        ) : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Confirmed > DateTime.Now.AddMinutes(1))
                yield return new ValidationResult("Invalid payment date", new string[] { nameof(Confirmed) });

        }
    }
}
