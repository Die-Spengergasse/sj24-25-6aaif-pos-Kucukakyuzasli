using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPG_Fachtheorie.Aufgabe1.Commands;
using SPG_Fachtheorie.Aufgabe1.Infrastructure;
using SPG_Fachtheorie.Aufgabe1.Model;
using SPG_Fachtheorie.Aufgabe3.Dtos;
using SPG_Fachtheorie.Aufgabe1.Services;

namespace SPG_Fachtheorie.Aufgabe3.Controllers
{
    [Route("api/[controller]")]  // --> /api/payments
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly PaymentService _service;

        public PaymentsController(PaymentService service)
        {
            _service = service;
        }

        /// <summary>
        /// GET /api/payments
        /// </summary>
        /// <returns></returns>
        /// api/
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<List<PaymentDto>> GetAllPayments(
            [FromQuery] int? cashDesk, [FromQuery] DateTime? dateFrom)
        {
            
            return Ok(_service.Payments
                .Where(p =>
                    cashDesk.HasValue
                        ? p.CashDesk.Number == cashDesk.Value : true)
                .Where(p =>
                    dateFrom.HasValue
                        ? p.PaymentDateTime >= dateFrom.Value : true)
                .Select(p => new PaymentDto(
                    p.Id, p.Employee.FirstName, p.Employee.LastName,
                    p.PaymentDateTime,
                    p.CashDesk.Number, p.PaymentType.ToString(),
                    p.PaymentItems.Sum(p => p.Amount)))
                .ToList());
        }
        //api/payments/1
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<PaymentDetailDto> GetPaymentDetail(int id)
        {
            var payment = _service.Payments
                .Where(p => p.Id == id)
                .Select(p => new PaymentDetailDto(
                    p.Id, p.Employee.FirstName, p.Employee.LastName,
                    p.CashDesk.Number, p.PaymentType.ToString(),
                    p.PaymentItems
                        .Select(pi => new PaymentItemDto(
                            pi.ArticleName, pi.Amount, pi.Price))
                        .ToList()
                    ))
                .FirstOrDefault();
            if (payment is null)
                return NotFound();
            return Ok(payment);
        }

        /// <summary>
        /// POST /api/payments
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult AddPayment(NewPaymentCommand cmd)
        {
            return CallServiceMethod(() => {
                var payment = _service.CreatePayment(cmd);
                return CreatedAtAction(nameof(AddPayment), new { Id = payment.Id });
            });
            
        }

        /// <summary>
        /// DELETE /api/payments/{id}?deleteItems=true|false
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult DeletePayment(int id)
        {
            return CallServiceMethod(() => {
                
                _service.DeletePayment(id);
                return NoContent();
            });
            
        }

        [HttpPut("/api/paymentItems/{id}")]
        public IActionResult UpdatePaymentItem(int id, UpdatePaymentItemCommand cmd)
        {
            return CallServiceMethod(() => {
                _service.UpdatePaymentItem(id, cmd);
                return Ok();
            });
            
        }

        [HttpPatch("{id}")]
        public IActionResult UpdateConfirmed(int id)
        {
            return CallServiceMethod(() => {
                _service.ConfirmPayment(id);
                return Ok();
            });
        }

        

        private IActionResult CallServiceMethod(Func<IActionResult> serviceCall) {
            try {
                return serviceCall();
            } catch (PaymentServiceException e) when (e.NotFoundException) {
                return Problem(e.Message, statusCode: 404);
            } catch (PaymentServiceException e) {
                return Problem(e.Message, statusCode: 400);
            }
        }
    }
}
