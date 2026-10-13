using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookingApi.Data;
using BookingApi.Models;

namespace BookingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AppointmentsController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Appointment>>> GetAppointments() =>
            await _context.Appointments.OrderBy(a => a.AppointmentDate).ToListAsync();

[HttpPost]
public async Task<ActionResult<Appointment>> PostAppointment(Appointment appointment)
{
    // Rule 1: No past bookings
    if (appointment.AppointmentDate < DateTime.UtcNow)
    {
        return BadRequest(new { message = "Cannot book appointments in the past." });
    }

    // Rule 2: Must be at least 1 hour from now
    if (appointment.AppointmentDate < DateTime.UtcNow.AddHours(1))
    {
        return BadRequest(new { message = "Appointments must be booked at least 1 hour in advance." });
    }

    // Rule 3: No overlapping appointments (Assumes each session lasts 60 minutes)
    var requestedStart = appointment.AppointmentDate;
    var requestedEnd = requestedStart.AddMinutes(60);

    var hasOverlap = await _context.Appointments.AnyAsync(a => 
        a.AppointmentDate < requestedEnd && a.AppointmentDate.AddMinutes(60) > requestedStart
    );

    if (hasOverlap)
    {
        return BadRequest(new { message = "This time slot overlaps with an existing appointment. You can book after the current appointment is over." });
    }

    _context.Appointments.Add(appointment);
    await _context.SaveChangesAsync();

    return CreatedAtAction(nameof(GetAppointments), new { id = appointment.Id }, appointment);
}

[HttpDelete("{id}")]
public async Task<IActionResult> DeleteAppointment(int id)
{
    var appointment = await _context.Appointments.FindAsync(id);
    if (appointment == null) return NotFound();
    _context.Appointments.Remove(appointment);
    await _context.SaveChangesAsync();
    return NoContent();
}}}