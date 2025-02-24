using Microsoft.EntityFrameworkCore;
using SPG_Fachtheorie.Aufgabe1.Infrastructure;
using SPG_Fachtheorie.Aufgabe1.Model;
using System;
using System.Linq;
using Xunit;

namespace SPG_Fachtheorie.Aufgabe1.Test
{
    [Collection("Sequential")]
    public class Aufgabe1Test
    {
        private AppointmentContext GetEmptyDbContext()
        {
            var options = new DbContextOptionsBuilder()
                .UseSqlite(@"Data Source=cash.db")
                .Options;

            var db = new AppointmentContext(options);
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            return db;
        }

        // Creates an empty DB in Debug\net8.0\cash.db
        [Fact]
        public void CreateDatabaseTest()
        {
            using var db = GetEmptyDbContext();
        }

        [Fact]
        public void AddCashierSuccessTest()
        {
            using var db = GetEmptyDbContext();
            Address address = new Address("Mustergasse", "Wien", "1020");
            Cashier cashier = new Cashier(1234, "Max", "Mustermann", address, "testest");
            db.Cashiers.Add(cashier);
            db.SaveChanges();
            var cashierFromDb = db.Employees.FirstOrDefault();
            Assert.NotNull(cashierFromDb);
            if (cashierFromDb == null) return;
            Assert.Equal(1234, cashierFromDb.RegistrationNumber);
        }

        [Fact]
        public void AddPaymentSuccessTest()
        {
            using var db = GetEmptyDbContext();
            var cashDesk = new CashDesk(1);
            db.CashDesks.Add(cashDesk);
            var address = new Address("Mustergasse", "Wien", "1020");
            var cashier = new Cashier(1234, "Max", "Mustermann", address, "testest");
            db.Employees.Add(cashier);
            var payment = new Payment(cashDesk, DateTime.UtcNow, PaymentType.Cash, cashier);
            db.Payments.Add(payment);
            db.SaveChanges();
            var paymentFromDb = db.Payments.FirstOrDefault();
            Assert.NotNull(paymentFromDb);
            if (paymentFromDb == null) return;
            Assert.Equal("Cash", paymentFromDb.PaymentType.ToString());

        }

        [Fact]
        public void EmployeeDiscriminatorSuccessTest()
        {
            using var db = GetEmptyDbContext();
            Address address = new Address("Mustergasse", "Wien", "1020");
            Cashier cashier = new Cashier(1234, "Max", "Mustermann", address, "testest");
            db.Cashiers.Add(cashier);
            var manager = new Manager(1244, "Maxie", "Mustermann", address, "testest");
            db.SaveChanges();
            var cashierFromDb = db.Employees.FirstOrDefault();
            Assert.NotNull(cashierFromDb);
            if (cashierFromDb == null) return;
            Assert.Equal("Cashier", cashierFromDb.Type);
        }
    }
}