﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;
using SPG_Fachtheorie.Aufgabe1.Infrastructure;
using SPG_Fachtheorie.Aufgabe1.Model;
using SPG_Fachtheorie.Aufgabe3.Commands;
using SPG_Fachtheorie.Aufgabe3.Dtos;

namespace SPG_Fachtheorie.Aufgabe3.Controllers
{
    [Route("api/[controller]")]  // [controller] --> Name vor "Controller" --> /api/employees
    [ApiController]              // Wird als controller berücksichtigt.
    public class EmployeesController : ControllerBase
    {
        private readonly AppointmentContext _db;
        public EmployeesController(AppointmentContext db)
        {
            _db = db;
        }

        // Verknüpfen von Adresse mit Methode.
        // HttpGet -> GET Request
        // Keine Adresse -> Adresse des Controllers
        // ==> Reagiert auf GET /api/employees
        //                  GET /api/employees?type=manager
        //                  GET /api/employees?type=MANAGER
        [HttpGet]
        public ActionResult<List<EmployeeDto>> GetAllEmployees([FromQuery] string? type)
        {
            return Ok(_db.Employees
                .Where(e => string.IsNullOrEmpty(type) ? true : e.Type.ToLower() == type.ToLower())
                .Select(e => new EmployeeDto(
                    e.RegistrationNumber, e.Type,
                    e.LastName,
                    e.FirstName))
                .ToList());
        }

        /// <summary>
        /// /api/employees/1001 --> 1001 wird in registrationNumber gelegt.
        /// </summary>
        /// <param name="registrationNumber"></param>
        /// <returns></returns>
        [HttpGet("{registrationNumber}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<EmployeeDetailDto> GetEmployeeById(int registrationNumber)
        {
            var employee = _db.Employees
                .Where(e => e.RegistrationNumber == registrationNumber)
                .Select(e => new EmployeeDetailDto(
                    e.RegistrationNumber, e.LastName,
                    e.FirstName, e.Address,
                    e.Payments.Count()))
                .AsNoTracking()
                .FirstOrDefault();
            if (employee is null)
                return NotFound();  // Return 404 not found.
            return Ok(employee);
        }

        /// <summary>
        /// Reagiert auf POST /api/manager
        /// </summary>
        [HttpPost("/api/manager")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult AddManager([FromBody] NewManagerCmd cmd)
        {
            var employee = new Manager(
                cmd.RegistrationNumber, cmd.FirstName, cmd.LastName,
                cmd.Address is not null
                    ? new Address(cmd.Address.Street, cmd.Address.Zip, cmd.Address.City)
                    : null,
                cmd.CarType);
            _db.Managers.Add(employee);
            try
            {
                _db.SaveChanges();  // INSERT INTO
            }
            catch (DbUpdateException e)
            {
                return Problem(
                    e.InnerException?.Message ?? e.Message,
                    statusCode: StatusCodes.Status400BadRequest);
            }
            return CreatedAtAction(nameof(AddManager), new { employee.RegistrationNumber });
        }


        /// <summary>
        /// DELETE /api/employee/{registrationNumber}
        /// Löscht den Employee. Da ON DELETE RESTRICT gesetzt wurde, müssen wir alle verbundenen
        /// Daten (Payments und PaymentItems) auch löschen.
        /// Ob das sinnvoll ist, muss immer geprüft werden. Eine Alternative wäre ein "soft delete",
        /// also ein Flag "visible" in Employee einfügen.
        /// </summary>
        [HttpDelete("{registrationNumber}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult DeleteEmployee(int registrationNumber)
        {
            var employee = _db.Employees.FirstOrDefault(e => e.RegistrationNumber == registrationNumber);
            if (employee is null) return NoContent();

            var paymentItems = _db.PaymentItems
                .Where(p => p.Payment.Employee.RegistrationNumber == registrationNumber)
                .ToList();

            var payments = _db.Payments
                .Where(p => p.Employee.RegistrationNumber == registrationNumber)
                .ToList();
            try
            {
                _db.PaymentItems.RemoveRange(paymentItems);
                _db.SaveChanges();

                _db.Payments.RemoveRange(payments);
                _db.SaveChanges();

                _db.Employees.Remove(employee);
                _db.SaveChanges();
            }
            catch (DbUpdateException e)
            {
                return Problem(
                    e.InnerException?.Message ?? e.Message,
                    statusCode: StatusCodes.Status400BadRequest);
            }
            catch (InvalidOperationException e)
            {
                return Problem(
                    e.InnerException?.Message ?? e.Message,
                    statusCode: StatusCodes.Status400BadRequest);
            }
            return NoContent();
        }
    }
}
