using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Security.Claims;
using UserAccountAPI.DTOs;
using UserAccountAPI.Models;
using UserAccountAPI.Services;

namespace UserAccountAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/Appointment")]
    public class AppointmentController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly ILogger<AppointmentController> _logger;

        public AppointmentController(IAppointmentService appointmentService, ILogger<AppointmentController> logger)
        {
            _appointmentService = appointmentService;
            _logger = logger;
        }

        [HttpGet("AllAppointments")]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetAllAppointments()
        {
            var appointments = await _appointmentService.GetAllAppointmentsAsync();
            return Ok(appointments);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AppointmentDto>> GetAppointmentById(int id)
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null)
                return NotFound($"Appointment with ID {id} not found");

            // Check if the user has access to this appointment
            if (User.IsInRole("Patient"))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "User ID is missing or invalid." });
                }

                // Check if the appointment belongs to this patient
                if (!await _appointmentService.IsAppointmentForUserAsync(id, userId))
                    return Forbid();
            }
            else if (User.IsInRole("Doctor"))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "User ID is missing or invalid." });
                }

                // Check if the appointment belongs to this doctor
                if (!await _appointmentService.IsAppointmentForDoctorAsync(id, userId))
                    return Forbid();
            }
            // Admin and Staff can access all appointments

            return Ok(appointment);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AppointmentDto>> UpdateAppointment(int id, AppointmentUpdateDto appointmentDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Check if the doctor has access to this appointment
                if (User.IsInRole("Doctor"))
                {
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    {
                        return Unauthorized(new { message = "User ID is missing or invalid." });
                    }

                    if (!await _appointmentService.IsAppointmentForDoctorAsync(id, userId))
                        return Forbid();
                }

                var appointment = await _appointmentService.UpdateAppointmentAsync(id, appointmentDto);
                if (appointment == null)
                    return NotFound($"Appointment with ID {id} not found");

                return Ok(appointment);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            var result = await _appointmentService.DeleteAppointmentAsync(id);
            if (!result)
                return NotFound($"Appointment with ID {id} not found");

            return NoContent();
        }

        [HttpPost("Create")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AppointmentDto>> CreateAppointment(AppointmentCreateDto appointmentDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // If user is a patient, make sure they're only creating appointments for themselves
                if (User.IsInRole("Patient"))
                {
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    {
                        return Unauthorized(new { message = "User ID is missing or invalid." });
                    }

                    // Check if the patient ID in appointmentDto matches the user
                    if (!await _appointmentService.IsPatientUserAsync(appointmentDto.PatientId, userId))
                        return Forbid();
                }

                var createdAppointment = await _appointmentService.CreateAppointmentAsync(appointmentDto);
                return CreatedAtAction(nameof(GetAppointmentById), new { id = createdAppointment.Id }, createdAppointment);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("GetAppointmentsByPatient/{patientId}")]
        [Authorize(Roles = "Admin,Doctor,Patient")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetAppointmentsByPatient(int patientId)
        {
            // If user is a patient, make sure they're only viewing their own appointments
            if (User.IsInRole("Patient"))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "User ID is missing or invalid." });
                }

                if (!await _appointmentService.IsPatientUserAsync(patientId, userId))
                    return Forbid();
            }

            var appointments = await _appointmentService.GetAppointmentsByPatientAsync(patientId);
            return Ok(appointments);
        }

        [HttpGet("GetAppointmentsByDoctor/{doctorId}")]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetAppointmentsByDoctor(int doctorId)
        {
            // If user is a doctor, make sure they're only viewing their own appointments
            if (User.IsInRole("Doctor"))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "User ID is missing or invalid." });
                }

                if (!await _appointmentService.IsDoctorUserAsync(doctorId, userId))
                    return Forbid();
            }

            var appointments = await _appointmentService.GetAppointmentsByDoctorAsync(doctorId);
            return Ok(appointments);
        }

        [HttpPut("UpdateStatus/{appointmentId}")]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AppointmentDto>> UpdateStatus(int appointmentId, AppointmentStatusUpdateDto statusDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // If user is a doctor, make sure they're only updating their own appointments
                if (User.IsInRole("Doctor"))
                {
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    {
                        return Unauthorized(new { message = "User ID is missing or invalid." });
                    }

                    if (!await _appointmentService.IsAppointmentForDoctorAsync(appointmentId, userId))
                        return Forbid();
                }

                var appointment = await _appointmentService.UpdateStatusAsync(appointmentId, statusDto);
                if (appointment == null)
                    return NotFound($"Appointment with ID {appointmentId} not found");

                return Ok(appointment);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("CheckDoctorAvailabilityVerbose")]
        [AllowAnonymous] // Anyone can check doctor availability
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<object>> CheckDoctorAvailabilityVerbose(
            [FromQuery] int doctorId,
            [FromQuery] DateTime date,
            [FromQuery] string startTime,
            [FromQuery] string endTime)
        {
            try
            {
                var isAvailable = await _appointmentService.CheckDoctorAvailabilityAsync(doctorId, date, startTime, endTime);
                return Ok(new
                {
                    isAvailable,
                    doctorId,
                    date,
                    startTime,
                    endTime
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("Filtering")]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<AppointmentDto>>> FilterAppointments(
            [FromQuery] DateTime? date = null,
            [FromQuery] int? doctorId = null,
            [FromQuery] int? patientId = null,
            [FromQuery] string status = null)
        {
            // If user is a doctor, restrict to their own appointments
            if (User.IsInRole("Doctor") && doctorId.HasValue)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "User ID is missing or invalid." });
                }

                if (!await _appointmentService.IsDoctorUserAsync(doctorId.Value, userId))
                    return Forbid();
            }

            // If user is a patient, restrict to their own appointments
            if (User.IsInRole("Patient") && patientId.HasValue)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "User ID is missing or invalid." });
                }

                if (!await _appointmentService.IsPatientUserAsync(patientId.Value, userId))
                    return Forbid();
            }

            var appointments = await _appointmentService.FilterAppointmentsAsync(date, doctorId, patientId, status);
            return Ok(appointments);
        }

        [HttpGet("MyAppointments")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetMyAppointments()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "User ID is missing or invalid." });
            }

            if (User.IsInRole("Patient"))
            {
                // Get appointments for this patient
                var patientId = await _appointmentService.GetPatientIdFromUserIdAsync(userId);
                if (patientId == null)
                    return NotFound(new { message = "Patient profile not found" });

                var appointments = await _appointmentService.GetAppointmentsByPatientAsync(patientId.Value);
                return Ok(appointments);
            }
            else if (User.IsInRole("Doctor"))
            {
                // Get appointments for this doctor
                var doctorId = await _appointmentService.GetDoctorIdFromUserIdAsync(userId);
                if (doctorId == null)
                    return NotFound(new { message = "Doctor profile not found" });

                var appointments = await _appointmentService.GetAppointmentsByDoctorAsync(doctorId.Value);
                return Ok(appointments);
            }

            // For Admin and Staff, return Forbid (they should use the other endpoints)
            return Forbid();
        }
    }
}