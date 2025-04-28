using System.ComponentModel.DataAnnotations;

namespace SPG_Fachtheorie.Aufgabe3.Commands {
    public record NewPaymentItemCommand (
           [StringLength(255, MinimumLength = 1, ErrorMessage = "Invalid article name")]
           string ArticleName,
           [Range(1, int.MaxValue, ErrorMessage = "Invalid amount")]
           int Amount,
           decimal Price,
           [Range(1, int.MaxValue, ErrorMessage = "Id out of range")]
           int PaymentId
        );
}
