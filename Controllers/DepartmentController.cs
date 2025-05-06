using UserAccountAPI.Models;
using UserAccountAPI.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using UserAccountAPI.Repositories;

namespace UserAccountAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DepartmentController : ControllerBase
    {
        private readonly IDeptRepository _repo;
        private readonly IDoctorRepository _doctorRepository;

        public DepartmentController(IDeptRepository repo, IDoctorRepository doctorRepository)
        {
            _repo = repo;
            _doctorRepository = doctorRepository;
        }

        /// <summary>
        /// Get all departments from the database
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllDepartments()
        {
            var departments = await _repo.GetAllDepartments();
            return Ok(departments);
        }

        /// <summary>
        /// Get a specific department by ID
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDepartment(int id)
        {
            var department = await _repo.GetDepartmentById(id);
            return department == null ? NotFound("Department not found") : Ok(department);
        }

        /// <summary>
        /// Update a department record
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateDepartment(int id, [FromBody] Department department)
        {
            try
            {
                var updatedDept = await _repo.UpdateDepartment(id, department);
                return updatedDept == null ? NotFound("Department not found") : Ok(updatedDept);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Delete a department - restricted to avoid data inconsistency
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            try
            {
                var result = await _repo.DeleteDepartment(id);
                return result ? NoContent() : NotFound("Department not found");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Add a department through the API - mainly for completeness
        /// Note: Primary department management happens in SQL Server Management Studio
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddDepartment([FromBody] Department department)
        {
            try
            {
                var newDept = await _repo.AddDepartment(department);
                return CreatedAtAction(nameof(GetDepartment), new { id = newDept.Id }, newDept);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Filter departments by name
        /// </summary>
        [HttpGet("filter")]
        public async Task<IActionResult> FilterDepartments([FromQuery] string? name)
        {
            var filtered = await _repo.FilterDepartments(name);
            return Ok(filtered);
        }

        /// <summary>
        /// Get all doctors in a specific department
        /// </summary>
        [HttpGet("{id:int}/doctors")]
        public async Task<IActionResult> GetDoctorsByDepartment(int id)
        {
            var department = await _repo.GetDepartmentById(id);
            if (department == null)
                return NotFound("Department not found");

            var doctors = await _doctorRepository.GetDoctorsByDepartment(id);
            return Ok(doctors);
        }
    }
}