using System.ComponentModel.DataAnnotations;

namespace SPG_Fachtheorie.Aufgabe3.Commands
{
    public record UpdatePaymentCmd([Range(1, 999999, ErrorMessage = "Invalid ID")]
        int Id,
        [StringLength(255, MinimumLength = 1, ErrorMessage = "Invalid Article Name")]
        string ArticleName,
        int Amount,
        decimal Price,
        int PaymentId,
        DateTime? LastUpdate
        ) : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Id <= 0)
                yield return new ValidationResult(
                    "Invalid ID",
                    new string[] { nameof(Id) });
            if (ArticleName == "")
                yield return new ValidationResult(
                    "Invalid Article Name",
                    new string[] { nameof(ArticleName) });
            if (Amount <= 0)
                yield return new ValidationResult(
                    "Invalid Amount",
                    new string[] { nameof(Amount) });
            if (Price <= 0)
                yield return new ValidationResult(
                    "Invalid Price",
                    new string[] { nameof(Price) });
            if (PaymentId <= 0)
                yield return new ValidationResult(
                    "Invalid Payment ID",
                    new string[] { nameof(PaymentId) });
        }
    }
}
