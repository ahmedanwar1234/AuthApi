using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserAccountAPI.Models;
using UserAccountAPI.Repositories;
using AutoMapper;
using UserAccountAPI.DTOs;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using UserAccountAPI.Common;

namespace MedicalAPI_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DoctorsController : ControllerBase
    {
        private readonly IDoctorRepository _doctorRepository;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public DoctorsController(
            IDoctorRepository doctorRepository,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager)
        {
            _doctorRepository = doctorRepository;
            _mapper = mapper;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: api/Doctors
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DoctorDto>>> GetDoctors()
        {
            var doctors = await _doctorRepository.GetAllDoctors();
            return _mapper.Map<List<DoctorDto>>(doctors);
        }

        // GET: api/Doctors/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DoctorDto>> GetDoctor(int id)
        {
            var doctor = await _doctorRepository.GetDoctorById(id);

            if (doctor == null)
            {
                return NotFound();
            }

            return _mapper.Map<DoctorDto>(doctor);
        }

        // GET: api/Doctors/department/5
        [HttpGet("department/{departmentId}")]
        public async Task<ActionResult<IEnumerable<DoctorDto>>> GetDoctorsByDepartment(int departmentId)
        {
            var doctors = await _doctorRepository.GetDoctorsByDepartment(departmentId);
            return _mapper.Map<List<DoctorDto>>(doctors);
        }

        // GET: api/Doctors/specialty/Cardiology
        [HttpGet("specialty/{specialty}")]
        public async Task<ActionResult<IEnumerable<DoctorDto>>> GetDoctorsBySpecialty(string specialty)
        {
            var doctors = await _doctorRepository.GetDoctorsBySpecialty(specialty);
            return _mapper.Map<List<DoctorDto>>(doctors);
        }

        // GET: api/Doctors/available
        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<DoctorDto>>> GetAvailableDoctors()
        {
            var doctors = await _doctorRepository.GetAvailableDoctorsToday();
            return _mapper.Map<List<DoctorDto>>(doctors);
        }

        // PUT: api/Doctors/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<ActionResult<DoctorDto>> PutDoctor(int id, DoctorUpdateDto doctorDto)
        {
            // Get the existing doctor first
            var existingDoctor = await _doctorRepository.GetDoctorById(id);

            if (existingDoctor == null)
            {
                return NotFound();
            }

            // Check if the user is a doctor updating their own profile
            if (User.IsInRole("Doctor") && !User.IsInRole("Admin"))
            {
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                {
                    return Unauthorized();
                }

                if (existingDoctor.UserId != userId)
                {
                    return Forbid();
                }
            }

            // Update the properties from the DTO
            existingDoctor.Name = doctorDto.Name;
            existingDoctor.Specialty = doctorDto.Specialty;
            existingDoctor.PhoneNumber = doctorDto.PhoneNumber;
            existingDoctor.IsAvailableToday = doctorDto.IsAvailableToday;
            existingDoctor.DepartmentId = doctorDto.DepartmentId;

            // Allow updating UserId only for admins
            if (User.IsInRole("Admin") && doctorDto.UserId.HasValue)
            {
                existingDoctor.UserId = doctorDto.UserId;
            }

            var updatedDoctor = await _doctorRepository.UpdateDoctor(existingDoctor);

            return _mapper.Map<DoctorDto>(updatedDoctor);
        }

        // POST: api/Doctors
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<DoctorDto>> PostDoctor(DoctorCreateDto doctorDto)
        {
            // Check if a doctor with this UserId already exists
            if (doctorDto.UserId.HasValue)
            {
                var existingDoctor = await _doctorRepository.GetDoctorByUserId(doctorDto.UserId);
                if (existingDoctor != null)
                {
                    return Conflict($"A doctor with User ID '{doctorDto.UserId}' already exists.");
                }
            }

            // Map the DTO to a Doctor entity
            var doctor = new Doctor
            {
                Name = doctorDto.Name,
                Specialty = doctorDto.Specialty,
                PhoneNumber = doctorDto.PhoneNumber,
                IsAvailableToday = doctorDto.IsAvailableToday,
                DepartmentId = doctorDto.DepartmentId,
                UserId = doctorDto.UserId
            };

            var newDoctor = await _doctorRepository.AddDoctor(doctor);

            return CreatedAtAction(
                nameof(GetDoctor),
                new { id = newDoctor.Id },
                _mapper.Map<DoctorDto>(newDoctor)
            );
        }

        // DELETE: api/Doctors/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteDoctor(int id)
        {
            var result = await _doctorRepository.DeleteDoctor(id);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }

        // PUT: api/Doctors/5/department/2
        [HttpPut("{id}/department/{departmentId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<DoctorDto>> AssignDepartment(int id, int departmentId)
        {
            var doctor = await _doctorRepository.AssignDepartment(id, departmentId);

            if (doctor == null)
            {
                return NotFound();
            }

            return _mapper.Map<DoctorDto>(doctor);
        }

        // PUT: api/Doctors/5/working-hours
        [HttpPut("{id}/working-hours")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<ActionResult<DoctorDto>> UpdateWorkingHours(int id, List<WorkingHoursDto> workingHoursDto)
        {
            // Check if the user is a doctor updating their own working hours
            if (User.IsInRole("Doctor") && !User.IsInRole("Admin"))
            {
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                {
                    return Unauthorized();
                }
                var existingDoctor = await _doctorRepository.GetDoctorById(id);

                if (existingDoctor == null)
                {
                    return NotFound();
                }

                if (existingDoctor.UserId != userId)
                {
                    return Forbid();
                }
            }

            // Convert DTOs to entities
            var workingHoursList = workingHoursDto.Select(wh => new WorkingHours
            {
                DoctorId = id,
                DayOfWeek = wh.DayOfWeek,
                StartTime = TimeSpan.Parse(wh.StartTime),
                EndTime = TimeSpan.Parse(wh.EndTime)
            }).ToList();

            var doctor = await _doctorRepository.UpdateWorkingHours(id, workingHoursList);

            if (doctor == null)
            {
                return NotFound();
            }

            return _mapper.Map<DoctorDto>(doctor);
        }

        // GET: api/Doctors/filter?specialty=Cardiology&available=true
        [HttpGet("filter")]
        public async Task<ActionResult<IEnumerable<DoctorDto>>> FilterDoctors([FromQuery] string specialty, [FromQuery] bool? available)
        {
            var doctors = await _doctorRepository.FilterDoctors(specialty, available);
            return _mapper.Map<List<DoctorDto>>(doctors);
        }

        // GET: api/Doctors/profile
        [HttpGet("profile")]
        [Authorize(Roles = "Doctor")]
        public async Task<ActionResult<DoctorDto>> GetDoctorProfile()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }
            var doctor = await _doctorRepository.GetDoctorByUserId(userId);

            if (doctor == null)
            {
                return NotFound();
            }

            return _mapper.Map<DoctorDto>(doctor);
        }

        // POST: api/Doctors/5/link-user/1
        [HttpPost("{doctorId}/link-user/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<DoctorDto>> LinkDoctorToUser(int doctorId, int userId)
        {
            // Check if user exists and has Doctor role
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Check if user already has Doctor role
            var userRoles = await _userManager.GetRolesAsync(user);
            if (!userRoles.Contains(UserRoles.Doctor))
            {
                // Add Doctor role if needed
                if (!await _roleManager.RoleExistsAsync(UserRoles.Doctor))
                {
                    await _roleManager.CreateAsync(new ApplicationRole { Name = UserRoles.Doctor });
                }

                await _userManager.AddToRoleAsync(user, UserRoles.Doctor);
            }

            // Link the doctor to the user
            var updatedDoctor = await _doctorRepository.LinkDoctorToUser(doctorId, userId);

            if (updatedDoctor == null)
            {
                return BadRequest("Failed to link doctor and user. Either doctor or user doesn't exist, or doctor already linked to another user.");
            }

            return _mapper.Map<DoctorDto>(updatedDoctor);
        }
    }
}