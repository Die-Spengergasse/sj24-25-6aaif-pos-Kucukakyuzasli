using Microsoft.EntityFrameworkCore;
using SPG_Fachtheorie.Aufgabe1.Commands;
using SPG_Fachtheorie.Aufgabe1.Infrastructure;
using SPG_Fachtheorie.Aufgabe1.Model;
using SPG_Fachtheorie.Aufgabe1.Services;
using SPG_Fachtheorie.Aufgabe3.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static System.Net.Mime.MediaTypeNames;

namespace SPG_Fachtheorie.Aufgabe1.Test
{
    public class PaymentServiceTests {
        private AppointmentContext GetEmptyDbContext() {
            var options = new DbContextOptionsBuilder()
                .UseSqlite(@"Data Source=cash.db")
                .Options;

            var db = new AppointmentContext(options);
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            return db;
        }

        public PaymentServiceTests() {
            
        }

        [Fact]
        public void CreatePaymentSuccessTest() {

            var dbContext = GetEmptyDbContext();
            var service = new PaymentService(dbContext);

            var cashDesk = new CashDesk(1);
            var paymentType = PaymentType.Cash;
            var employee = new Cashier(1, "John", "Doe", new DateOnly(1990, 1, 1), 3000, null, "General");

            dbContext.CashDesks.Add(cashDesk);
            dbContext.Employees.Add(employee);
            dbContext.SaveChanges();

            var command = new NewPaymentCommand(
                    cashDesk.Number,
                    paymentType.ToString(),
                    employee.RegistrationNumber
                );

            var payment = service.CreatePayment(command);

            Assert.NotNull(payment);
            Assert.Equal(cashDesk, payment.CashDesk);
            Assert.Equal(employee, payment.Employee);
            Assert.Equal(paymentType, payment.PaymentType);
        }

        [Theory]
        [InlineData(0, "Cash", 1, "Invalid cash desk")] // Invalid cash desk number
        [InlineData(1, "InvalidType", 1, "Invalid payment type")] // Invalid payment type
        [InlineData(1, "Cash", 0, "Invalid employee")] // Invalid employee registration number
        [InlineData(1, "CreditCard", 1, "Insufficient rights to create a credit card payment")] // Insufficient rights for credit card payment
        [InlineData(999, "Cash", 1, "Invalid cash desk")] // Non-existent cash desk
        [InlineData(1, "Cash", 999, "Invalid employee")] // Non-existent employee
        public void CreatePaymentExceptionsTest(int cashDeskNumber, string paymentType, int employeeNumber, string expectedErrorMessage) {
            var dbContext = GetEmptyDbContext();
            var service = new PaymentService(dbContext);

            var cashDesk = new CashDesk(1);
            var employee = new Cashier(1, "John", "Doe", new DateOnly(1990, 1, 1), 3000, null, "General");
            var manager = new Manager(2, "Jane", "Smith", new DateOnly(1985, 1, 1), 4000, null, "Manager");
            dbContext.CashDesks.Add(cashDesk);
            dbContext.Employees.Add(employee);
            dbContext.Employees.Add(manager);
            dbContext.SaveChanges();

            PaymentServiceException argumentException = Assert.Throws<PaymentServiceException>(() =>
                service.CreatePayment(new NewPaymentCommand(cashDeskNumber, paymentType, employeeNumber)));
            var exception = argumentException;

            Assert.Equal(expectedErrorMessage, exception.Message);
        }

        

        [Fact]
        public void ConfirmPaymentSuccessTest() {
            var dbContext = GetEmptyDbContext();
            var service = new PaymentService(dbContext);

            var cashDesk = new CashDesk(1);
            var employee = new Cashier(1, "John", "Doe", new DateOnly(1990, 1, 1), 3000, null, "General");
            var payment = new Payment(cashDesk, DateTime.UtcNow, employee, PaymentType.Cash) {
                Id = 1,
                Confirmed = null
            };

            dbContext.CashDesks.Add(cashDesk);
            dbContext.Employees.Add(employee);
            dbContext.Payments.Add(payment);
            dbContext.SaveChanges();

            service.ConfirmPayment(payment.Id);

            var updatedPayment = dbContext.Payments.First(p => p.Id == payment.Id);
            Assert.NotNull(updatedPayment.Confirmed);
            Assert.True(updatedPayment.Confirmed <= DateTime.UtcNow);
        }

        [Theory]
        [InlineData(999, "Payment not found")] // Non-existent payment
        [InlineData(1, "Payment already confirmed")] // Payment already confirmed
        public void ConfirmPaymentExceptionsTest(int paymentId, string expectedErrorMessage) {
            var dbContext = GetEmptyDbContext();
            var service = new PaymentService(dbContext);

            var cashDesk = new CashDesk(1);
            var employee = new Cashier(1, "John", "Doe", new DateOnly(1990, 1, 1), 3000, null, "General");
            var payment = new Payment(cashDesk, DateTime.UtcNow, employee, PaymentType.Cash) {
                Id = 1,
                Confirmed = paymentId == 1 ? DateTime.UtcNow : null
            };

            dbContext.CashDesks.Add(cashDesk);
            dbContext.Employees.Add(employee);
            dbContext.Payments.Add(payment);
            dbContext.SaveChanges();

            PaymentServiceException exception = Assert.Throws<PaymentServiceException>(() =>
                service.ConfirmPayment(paymentId));

            Assert.Equal(expectedErrorMessage, exception.Message);
        }

        
        

        [Fact]
        public void AddPaymentItemSuccessTest() {
            var dbContext = GetEmptyDbContext();
            var service = new PaymentService(dbContext);

            var cashDesk = new CashDesk(1);
            var employee = new Cashier(1, "John", "Doe", new DateOnly(1990, 1, 1), 3000, null, "General");
            var payment = new Payment(cashDesk, DateTime.UtcNow, employee, PaymentType.Cash) {
                Id = 1
            };

            dbContext.CashDesks.Add(cashDesk);
            dbContext.Employees.Add(employee);
            dbContext.Payments.Add(payment);
            dbContext.SaveChanges();

            var command = new NewPaymentItemCommand("Test Article", 2, 15.0m, payment.Id);

            service.AddPaymentItem(command);

            var paymentItem = dbContext.PaymentItems.FirstOrDefault();
            Assert.NotNull(paymentItem);
            Assert.Equal("Test Article", paymentItem.ArticleName);
            Assert.Equal(2, paymentItem.Amount);
            Assert.Equal(15.0m, paymentItem.Price);
            Assert.Equal(payment, paymentItem.Payment);
        }

        [Theory]
        [InlineData("x",100,30.0 ,2, "Payment not found")]
        [InlineData("", 100, 30.0, 1, "Invalid article name")]
        [InlineData("x", 0, 30.0, 1, "Invalid amount")]
        [InlineData("x", 0, 30.0, 0, "Id out of range")]

        public void AddPaymentItemTextExceptionsTest(string title, int amount, decimal price, int paymentId, string exception) {
            var dbContext = GetEmptyDbContext();
            var service = new PaymentService(dbContext);

            var cashDesk = new CashDesk(1);
            var employee = new Cashier(1, "John", "Doe", new DateOnly(1990, 1, 1), 3000, null, "General");
            var payment = new Payment(cashDesk, DateTime.UtcNow, employee, PaymentType.Cash) {
                Id = 1
            };

            dbContext.CashDesks.Add(cashDesk);
            dbContext.Employees.Add(employee);
            dbContext.Payments.Add(payment);
            dbContext.SaveChanges();

            var command = new NewPaymentItemCommand(title, amount, price, paymentId);

            PaymentServiceException e = Assert.Throws<PaymentServiceException>(() =>
                service.AddPaymentItem(command));

            Assert.Equal(exception, e.Message);
        }

        [Fact]
        public void DeletePaymentTest() {
            var dbContext = GetEmptyDbContext();
            var service = new PaymentService(dbContext);

            var cashDesk = new CashDesk(1);
            var employee = new Cashier(1, "John", "Doe", new DateOnly(1990, 1, 1), 3000, null, "General");
            var payment = new Payment(cashDesk, DateTime.UtcNow, employee, PaymentType.Cash) {
                Id = 1
            };

            dbContext.CashDesks.Add(cashDesk);
            dbContext.Employees.Add(employee);
            dbContext.Payments.Add(payment);
            dbContext.SaveChanges();

            service.DeletePayment(1);

            
            Assert.Null(dbContext.Payments.Where(p => p.Id == 1).FirstOrDefault());
            Assert.Null(dbContext.PaymentItems.Where(p => p.Payment.Id == 1).FirstOrDefault());

        }
        [Theory]
        [InlineData(2, "Payment not found")]
        public void DeletePaymentExceptionsTest(int id, string exception) {
            var dbContext = GetEmptyDbContext();
            var service = new PaymentService(dbContext);

            var cashDesk = new CashDesk(1);
            var employee = new Cashier(1, "John", "Doe", new DateOnly(1990, 1, 1), 3000, null, "General");
            var payment = new Payment(cashDesk, DateTime.UtcNow, employee, PaymentType.Cash) {
                Id = 1
            };

            dbContext.CashDesks.Add(cashDesk);
            dbContext.Employees.Add(employee);
            dbContext.Payments.Add(payment);
            dbContext.SaveChanges();


            PaymentServiceException e = Assert.Throws<PaymentServiceException>(() => service.DeletePayment(id));
            Assert.Equal(exception, e.Message);
            
        }
    }
}
