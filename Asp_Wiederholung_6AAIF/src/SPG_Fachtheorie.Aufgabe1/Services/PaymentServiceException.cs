using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPG_Fachtheorie.Aufgabe1.Services
{
    public class PaymentServiceException :Exception
    {
        public bool NotFoundException { get; set; }
        public PaymentServiceException() {
        }
        public PaymentServiceException(string? message) : base(message) {
        }
        public PaymentServiceException(string? message, Exception? innerException) : base(message, innerException) {
        }
    }
}
