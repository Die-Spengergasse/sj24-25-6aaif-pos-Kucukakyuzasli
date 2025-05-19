using Spg.Fachtheorie.Aufgabe3.API.Test;
using SPG_Fachtheorie.Aufgabe1.Model;
using SPG_Fachtheorie.Aufgabe3.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace SPG_Fachtheorie.Aufgabe3.Test {
    public class PaymentsControllerTest {
        [Theory]
        [InlineData(1, null, 2)]
        [InlineData(0, "2024-05-13", 2)]
        [InlineData(1, "2024-05-13", 1)]
        public async Task GetAllPayments_FilteredTest(int cashDesk, string? dateFrom, int expectedCount) {
            // ARRANGE
            var factory = new TestWebApplicationFactory();
            factory.InitializeDatabase(db => {
                var employee1 = new Cashier(1, "FN", "LN", new DateOnly(2004, 3, 2), 3000M, null, "Feinkost");
                var cashdesk1 = new CashDesk(1);
                var cashdesk2 = new CashDesk(2);
                var payment1 = new Payment(cashdesk1, new DateTime(2024, 5, 13), employee1, PaymentType.Cash);
                var payment2 = new Payment(cashdesk1, new DateTime(2024, 4, 14), employee1, PaymentType.Cash);
                var payment3 = new Payment(cashdesk2, new DateTime(2024, 5, 13), employee1, PaymentType.Cash);
                db.Employees.Add(employee1);
                db.CashDesks.AddRange(cashdesk1, cashdesk2);
                db.Payments.AddRange(payment1, payment2, payment3);
                db.SaveChanges();
            });

            // Build query string
            var query = "";
            if (cashDesk != 0)
                query += $"cashDesk={cashDesk}";
            if (dateFrom != null)
                query += (query.Length > 0 ? "&" : "") + $"dateFrom={dateFrom}";

            // ACT
            var (statusCode, payments) = await factory.GetHttpContent<List<PaymentDto>>($"/api/payments{(query.Length > 0 ? "?" + query : "")}");

            // ASSERT
            Assert.True(statusCode == HttpStatusCode.OK);
            Assert.NotNull(payments);
            Assert.Equal(expectedCount, payments.Count);
            if (cashDesk != 0)
                Assert.True(payments.All(p => p.CashDeskNumber == cashDesk));
            if (dateFrom != null)
                Assert.True(payments.All(p => p.PaymentDateTime == (dateFrom != null ? DateTime.Parse(dateFrom) : p.PaymentDateTime)));
                
        }

        [Theory]
        [InlineData(1, HttpStatusCode.OK)]
        [InlineData(999, HttpStatusCode.NotFound)]
        public async Task GetPaymentById_StatusCodesTest(int id, HttpStatusCode expectedStatus) {
            // ARRANGE
            var factory = new TestWebApplicationFactory();
            factory.InitializeDatabase(db => {
                var employee1 = new Cashier(1, "FN", "LN", new DateOnly(2004, 3, 2), 3000M, null, "Feinkost");
                var cashdesk1 = new CashDesk(1);
                var payment1 = new Payment(cashdesk1, new DateTime(2024, 5, 13), employee1, PaymentType.Cash);
                db.Employees.Add(employee1);
                db.CashDesks.Add(cashdesk1);
                db.Payments.Add(payment1);
                db.SaveChanges();
            });

            // ACT
            var (statusCode, content) = await factory.GetHttpContent<PaymentDto>($"/api/payments/{id}");

            // ASSERT
            Assert.Equal(expectedStatus, statusCode);
            if (expectedStatus == HttpStatusCode.OK)
                Assert.NotNull(content);
        }


        [Theory]
        [InlineData(1, HttpStatusCode.OK)]
        [InlineData(999, HttpStatusCode.NotFound)]
        [InlineData(2, HttpStatusCode.BadRequest)]
        public async Task PatchPaymentById_ConfirmPaymentTest(int id, HttpStatusCode expectedStatus) {
            // ARRANGE
            var factory = new TestWebApplicationFactory();
            factory.InitializeDatabase(db => {
                var employee1 = new Cashier(1, "FN", "LN", new DateOnly(2004, 3, 2), 3000M, null, "Feinkost");
                var cashdesk1 = new CashDesk(1);
                var cashdesk2 = new CashDesk(2);
                var payment1 = new Payment(cashdesk1, new DateTime(2024, 5, 13), employee1, PaymentType.Cash);
                var payment2 = new Payment(cashdesk2, new DateTime(2024, 5, 15), employee1, PaymentType.Cash) {
                    Confirmed = DateTime.UtcNow
                };
                db.Employees.Add(employee1);
                db.CashDesks.Add(cashdesk1);
                db.CashDesks.Add(cashdesk2);
                db.Payments.Add(payment1);
                db.Payments.Add(payment2);
                db.SaveChanges();
            });

            
            // ACT
            var (statusCode, content) = await factory.PatchHttpContent($"/api/payments/{id}", new {});

            // ASSERT
            Assert.Equal(expectedStatus, statusCode);

        }

        [Theory]
        [InlineData(1, HttpStatusCode.NoContent)]
        [InlineData(999, HttpStatusCode.NotFound)]
        public async Task DeletePaymentById_StatusCodesTest(int id, HttpStatusCode expectedStatus) {
            // ARRANGE
            var factory = new TestWebApplicationFactory();
            factory.InitializeDatabase(db => {
                var employee1 = new Cashier(1, "FN", "LN", new DateOnly(2004, 3, 2), 3000M, null, "Feinkost");
                var cashdesk1 = new CashDesk(1);
                var payment1 = new Payment(cashdesk1, new DateTime(2024, 5, 13), employee1, PaymentType.Cash);
                db.Employees.Add(employee1);
                db.CashDesks.Add(cashdesk1);
                db.Payments.Add(payment1);
                db.SaveChanges();
            });

            // ACT
            var response = await factory.Client.DeleteAsync($"/api/payments/{id}");

            // ASSERT
            Assert.Equal(expectedStatus, response.StatusCode);
        }
    }
}
