using System.ComponentModel.DataAnnotations;

namespace SPG_Fachtheorie.Aufgabe3.Commands {
    //"cashDeskNumber": 1,
    //"paymentDateTime": "2025-03-07T14:00:00",
    //"paymentType": "Cash"|"Maestro"|"Creditcard",
    //"employeeRegistrationNumber": 1001
    public record NewPaymentCommand(
            [Range(1, 999999, ErrorMessage = "Invalid cash desk numbner")]
                int CashDeskNumber,
                DateTime PaymentDateTime,
            [StringLength(255, MinimumLength = 1, ErrorMessage = "Invalid payment type")]
                string PaymentType,
            [Range(1, 999999, ErrorMessage = "Invalid registration number")]
                int EmployeeRegistrationNumber
        ) : IValidatableObject {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            if (PaymentDateTime > DateTime.Now.AddMinutes(1)) {
                yield return new ValidationResult("Payment date cannot be more than 1 minute in the future", new[] { nameof(PaymentDateTime) });
            }
        }
    }
}
