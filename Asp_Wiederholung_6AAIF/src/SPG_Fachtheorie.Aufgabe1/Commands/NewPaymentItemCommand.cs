using System.ComponentModel.DataAnnotations;

namespace SPG_Fachtheorie.Aufgabe3.Commands {
    public record NewPaymentItemCommand (
           [StringLength(255, MinimumLength = 1, ErrorMessage = "Invalid car")]
           string ArticleName,
           [Range(1, int.MaxValue, ErrorMessage = "Invalid cash desk number")]
           int Amount,
           decimal Price,
           [Range(1, int.MaxValue, ErrorMessage = "Invalid cash desk number")]
           int PaymentId
        );
}
