using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserAccountAPI.DTOs;
using UserAccountAPI.Models;
using UserAccountAPI.Repositories.Interfaces;
using AutoMapper;
using System.Linq;
using System.Security.Claims;

namespace UserAccountAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PatientsController : ControllerBase
    {
        private readonly IPatientRepository _patientRepository;
        private readonly IMapper _mapper;

        public PatientsController(IPatientRepository patientRepository, IMapper mapper)
        {
            _patientRepository = patientRepository;
            _mapper = mapper;
        }

        // GET: api/Patients
        [HttpGet]
        [Authorize(Roles = "Admin,Doctor,Patient")]
        public async Task<ActionResult<IEnumerable<PatientDTO>>> GetPatients()
        {
            var patients = await _patientRepository.GetAllPatients();
            return Ok(_mapper.Map<List<PatientDTO>>(patients));
        }

        // GET: api/Patients/5
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Doctor,Patient")]
        public async Task<ActionResult<PatientDTO>> GetPatient(int id)
        {
            var patient = await _patientRepository.GetPatientById(id);

            if (patient == null)
            {
                return NotFound();
            }

            // Allow patients to view their own profile
            if (User.IsInRole("Patient") && !User.IsInRole("Admin") && !User.IsInRole("Doctor"))
            {
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                {
                    return Unauthorized("Invalid user ID.");
                }
                if (patient.UserId != userId)
                {
                    return Forbid();
                }
            }

            return Ok(_mapper.Map<PatientDTO>(patient));
        }

        // PUT: api/Patients/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Doctor,Patient")]
        public async Task<IActionResult> UpdatePatient(int id, PatientUpdateDTO patientDto)
        {
            var existingPatient = await _patientRepository.GetPatientById(id);

            if (existingPatient == null)
            {
                return NotFound();
            }

            // Allow patients to update only their own profile
            if (User.IsInRole("Patient") && !User.IsInRole("Admin") && !User.IsInRole("Doctor"))
            {
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                {
                    return Unauthorized("Invalid user ID.");
                }
                if (existingPatient.UserId != userId)
                {
                    return Forbid();
                }
            }

            // Update the patient with values from the DTO
            if (patientDto.FullName != null)
                existingPatient.FullName = patientDto.FullName;

            if (patientDto.DateOfBirth.HasValue)
                existingPatient.DateOfBirth = patientDto.DateOfBirth.Value;

            if (patientDto.Gender != null)
                existingPatient.Gender = patientDto.Gender;

            if (patientDto.PhoneNumber != null)
                existingPatient.PhoneNumber = patientDto.PhoneNumber;

            await _patientRepository.UpdatePatient(existingPatient);

            return NoContent();
        }

        // DELETE: api/Patients/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeletePatient(int id)
        {
            var result = await _patientRepository.DeletePatient(id);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }

        // GET: api/Patients/year/2023
        [HttpGet("year/{year}")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<ActionResult<IEnumerable<PatientDTO>>> GetPatientsByYear(int year)
        {
            var patients = await _patientRepository.GetPatientsAdmittedInYear(year);
            return Ok(_mapper.Map<List<PatientDTO>>(patients));
        }

        // GET: api/Patients/age-range?minAge=20&maxAge=40
        [HttpGet("age-range")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<ActionResult<IEnumerable<PatientDTO>>> GetPatientsByAgeRange([FromQuery] int minAge, [FromQuery] int maxAge)
        {
            var patients = await _patientRepository.GetPatientsByAgeRange(minAge, maxAge);
            return Ok(_mapper.Map<List<PatientDTO>>(patients));
        }

        // GET: api/Patients/with-appointments
        [HttpGet("with-appointments")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<ActionResult<IEnumerable<PatientDTO>>> GetPatientsWithAppointments()
        {
            var patients = await _patientRepository.GetPatientsWithAppointments();
            return Ok(_mapper.Map<List<PatientDTO>>(patients));
        }

        // GET: api/Patients/gender-count
        [HttpGet("gender-count")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<ActionResult<Dictionary<string, int>>> GetPatientCountByGender()
        {
            var counts = await _patientRepository.CountPatientsByGender();
            return Ok(counts);
        }

        // GET: api/Patients/search?term=John
        [HttpGet("search")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<ActionResult<IEnumerable<PatientDTO>>> SearchPatients([FromQuery] string term)
        {
            var patients = await _patientRepository.SearchPatients(term);
            return Ok(_mapper.Map<List<PatientDTO>>(patients));
        }

        // PUT: api/Patients/5/phone
        [HttpPut("{id}/phone")]
        [Authorize(Roles = "Admin,Doctor,Patient")]
        public async Task<ActionResult<PatientDTO>> UpdatePatientPhone(int id, [FromBody] string phoneNumber)
        {
            var existingPatient = await _patientRepository.GetPatientById(id);

            if (existingPatient == null)
            {
                return NotFound();
            }

            // Allow patients to update only their own phone number
            if (User.IsInRole("Patient") && !User.IsInRole("Admin") && !User.IsInRole("Doctor"))
            {
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                {
                    return Unauthorized("Invalid user ID.");
                }
                if (existingPatient.UserId != userId)
                {
                    return Forbid();
                }
            }

            var updatedPatient = await _patientRepository.UpdatePatientPhone(id, phoneNumber);

            if (updatedPatient == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<PatientDTO>(updatedPatient));
        }

        // POST: api/Patients/filter
        [HttpPost("filter")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<ActionResult<IEnumerable<PatientDTO>>> FilterPatients([FromBody] PatientFilterDTO filter)
        {
            var patients = await _patientRepository.FilterPatients(filter);
            return Ok(_mapper.Map<List<PatientDTO>>(patients));
        }

        // GET: api/Patients/profile
        [HttpGet("profile")]
        [Authorize(Roles = "Patient")]
        public async Task<ActionResult<PatientDTO>> GetPatientProfile()
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            {
                return Unauthorized("Invalid user ID.");
            }

            var patientProfile = await _patientRepository.GetByUserIdAsync(userId);

            if (patientProfile == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<PatientDTO>(patientProfile));
        }
    }
}