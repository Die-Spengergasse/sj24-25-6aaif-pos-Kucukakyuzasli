using Microsoft.EntityFrameworkCore;
using SPG_Fachtheorie.Aufgabe1.Commands;
using SPG_Fachtheorie.Aufgabe1.Infrastructure;
using SPG_Fachtheorie.Aufgabe1.Model;
using SPG_Fachtheorie.Aufgabe3.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPG_Fachtheorie.Aufgabe1.Services
{
    public class PaymentService
    {
        private readonly AppointmentContext _db;

        public PaymentService(AppointmentContext db) {
            _db = db;
        }
        public IQueryable<Payment> Payments => _db.Payments.AsQueryable();
        public IQueryable<PaymentItem> PaymentItems => _db.PaymentItems.AsQueryable();

        private void SaveOrThrow() {
            try {
                _db.SaveChanges();  // INSERT INTO
            } catch (DbUpdateException e) {
                throw new PaymentServiceException(e.InnerException?.Message ?? e.Message);
            }
        }

        public Payment CreatePayment(NewPaymentCommand cmd) {
            var cashDesk = _db.CashDesks.FirstOrDefault(c => c.Number == cmd.CashDeskNumber);
            if (cashDesk is null) throw new PaymentServiceException("Invalid cash desk");
            var employee = _db.Employees.FirstOrDefault(e => e.RegistrationNumber == cmd.EmployeeRegistrationNumber);
            if (employee is null) throw new PaymentServiceException("Invalid employee");

            if (!Enum.TryParse<PaymentType>(cmd.PaymentType, out var paymentType))
                throw new PaymentServiceException("Invalid payment type");
            var payment = new Payment(
                cashDesk, DateTime.UtcNow, employee, paymentType);
            if (payment.PaymentType == PaymentType.CreditCard && payment.Employee.Type != "Manager")
                throw new PaymentServiceException("Insufficient rights to create a credit card payment");
            _db.Payments.Add(payment);
            SaveOrThrow();
            return payment;
        }
        public void ConfirmPayment(int paymentId) {
            var payment = _db.Payments.FirstOrDefault(p => p.Id == paymentId);
            if (payment is null)
                throw new PaymentServiceException("Payment not found") { NotFoundException = true };
            if (payment.Confirmed.HasValue)
                throw new PaymentServiceException("Payment already confirmed");
            payment.Confirmed = DateTime.UtcNow;
            SaveOrThrow();
        }
        public void DeletePayment(int paymentId) {
            var payment = _db.Payments.FirstOrDefault(p => p.Id == paymentId);
            if (payment is null)
                throw new PaymentServiceException("Payment not found") { NotFoundException = true };
            var paymentItems = _db.PaymentItems.Where(p => p.Payment.Id == paymentId).ToList();
            if (paymentItems.Any()) {
                _db.PaymentItems.RemoveRange(paymentItems);
                SaveOrThrow();
            }
            _db.Payments.Remove(payment);
            SaveOrThrow();
        }
        public void AddPaymentItem(NewPaymentItemCommand cmd) {
            var payment = _db.Payments.FirstOrDefault(p => p.Id == cmd.PaymentId);
            if (payment is null)
                throw new PaymentServiceException("Payment not found") { NotFoundException = true };
            var paymentItem = new PaymentItem(cmd.ArticleName, cmd.Amount, cmd.Price, payment);
            _db.PaymentItems.Add(paymentItem);
            SaveOrThrow();

        }
        public void UpdatePaymentItem(int id, UpdatePaymentItemCommand cmd) {
            if (id != cmd.Id)
                throw new PaymentServiceException("Invalid payment item ID");

            var paymentItem = _db.PaymentItems.FirstOrDefault(p => p.Id == id);
            if (paymentItem is null)
                throw new PaymentServiceException("Payment Item not found") { NotFoundException = true};
            var payment = _db.Payments.FirstOrDefault(p => p.Id == cmd.PaymentId);
            if (payment is null)
                throw new PaymentServiceException("Payment not found") { NotFoundException = true };
            if (paymentItem.LastUpdated == cmd.LastUpdated)
                throw new PaymentServiceException("Payment item has changed");

            paymentItem.ArticleName = cmd.ArticleName;
            paymentItem.Amount = cmd.Amount;
            paymentItem.Price = cmd.Price;
            paymentItem.LastUpdated = DateTime.UtcNow;
            SaveOrThrow();

        }
    }
}
