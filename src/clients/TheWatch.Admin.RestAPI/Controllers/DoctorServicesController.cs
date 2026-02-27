using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheWatch.Contracts.DoctorServices;
using TheWatch.Contracts.DoctorServices.Models;

namespace TheWatch.Admin.RestAPI.Controllers;

[ApiController]
[Route("api/admin/doctors")]
[Authorize(Policy = "AdminOnly")]
public class DoctorServicesController(IDoctorServicesClient doctors) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListDoctors([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await doctors.ListDoctorsAsync(page, pageSize, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDoctor(Guid id, CancellationToken ct)
        => Ok(await doctors.GetDoctorAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> CreateDoctor([FromBody] CreateDoctorProfileRequest request, CancellationToken ct)
        => Ok(await doctors.CreateDoctorAsync(request, ct));

    [HttpGet("search")]
    public async Task<IActionResult> SearchDoctors([FromQuery] string? specialization = null, [FromQuery] double? lat = null, [FromQuery] double? lon = null, [FromQuery] double? radius = null, [FromQuery] bool? acceptingOnly = null, CancellationToken ct = default)
        => Ok(await doctors.SearchDoctorsAsync(new DoctorSearchQuery(specialization, lat, lon, radius, acceptingOnly), ct));

    [HttpPost("appointments")]
    public async Task<IActionResult> BookAppointment([FromBody] BookAppointmentRequest request, CancellationToken ct)
        => Ok(await doctors.BookAppointmentAsync(request, ct));

    [HttpGet("appointments/{id:guid}")]
    public async Task<IActionResult> GetAppointment(Guid id, CancellationToken ct)
        => Ok(await doctors.GetAppointmentAsync(id, ct));

    [HttpPut("appointments/{id:guid}/status")]
    public async Task<IActionResult> UpdateAppointmentStatus(Guid id, [FromBody] UpdateAppointmentStatusRequest request, CancellationToken ct)
        => Ok(await doctors.UpdateAppointmentStatusAsync(id, request, ct));

    [HttpPut("appointments/{id:guid}/reschedule")]
    public async Task<IActionResult> Reschedule(Guid id, [FromBody] RescheduleRequest request, CancellationToken ct)
        => Ok(await doctors.RescheduleAsync(id, request, ct));

    [HttpGet("appointments/{appointmentId:guid}/telehealth")]
    public async Task<IActionResult> GetTelehealthSession(Guid appointmentId, CancellationToken ct)
        => Ok(await doctors.GetTelehealthSessionAsync(appointmentId, ct));
}
