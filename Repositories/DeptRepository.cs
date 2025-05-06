// DeptRepository.cs
using UserAccountAPI.Models;
using Microsoft.EntityFrameworkCore;
using UserAccountAPI.Data;
using UserAccountAPI.Repositories.Interfaces;

namespace MedicalAPI_1.Repositories
{
    public class DeptRepository : IDeptRepository
    {
        private readonly ApplicationDbContext _context;

        public DeptRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all departments from the database
        /// </summary>
        public async Task<List<Department>> GetAllDepartments()
        {
            return await _context.Departments.ToListAsync();
        }

        /// <summary>
        /// Gets a specific department by ID
        /// </summary>
        public async Task<Department?> GetDepartmentById(int id)
        {
            return await _context.Departments.FirstOrDefaultAsync(d => d.Id == id);
        }

        /// <summary>
        /// Adds a department record - note: primarily for API completeness
        /// Most departments should be added directly via SQL Server Management Studio
        /// </summary>
        public async Task<Department> AddDepartment(Department department)
        {
            // Check if department with same name already exists
            var existingDept = await _context.Departments
                .FirstOrDefaultAsync(d => d.Name.ToLower() == department.Name.ToLower());

            if (existingDept != null)
            {
                throw new InvalidOperationException($"Department with name '{department.Name}' already exists.");
            }

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();
            return department;
        }

        /// <summary>
        /// Updates a department record - only updates the fields in the application
        /// Will not conflict with other data management done in SQL Server Studio
        /// </summary>
        public async Task<Department?> UpdateDepartment(int id, Department department)
        {
            var existingDept = await GetDepartmentById(id);
            if (existingDept == null) return null;

            // Check if new name conflicts with existing department (except itself)
            if (!string.Equals(existingDept.Name, department.Name, StringComparison.OrdinalIgnoreCase))
            {
                var duplicateName = await _context.Departments
                    .AnyAsync(d => d.Id != id && d.Name.ToLower() == department.Name.ToLower());

                if (duplicateName)
                {
                    throw new InvalidOperationException($"Department with name '{department.Name}' already exists.");
                }
            }

            existingDept.Name = department.Name;
            existingDept.Description = department.Description;

            await _context.SaveChangesAsync();
            return existingDept;
        }

        /// <summary>
        /// Deletes a department - use with caution as SQL-created departments will also be removed
        /// </summary>
        public async Task<bool> DeleteDepartment(int id)
        {
            var department = await GetDepartmentById(id);
            if (department == null) return false;

            // Check if department has associated doctors
            var hasDoctors = await _context.Set<Doctor>().AnyAsync(d => d.DepartmentId == id);
            if (hasDoctors)
            {
                throw new InvalidOperationException("Cannot delete department with associated doctors.");
            }

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Filters departments by name
        /// </summary>
        public async Task<List<Department>> FilterDepartments(string? name)
        {
            var query = _context.Departments.AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(d => d.Name.Contains(name));
            }

            return await query.ToListAsync();
        }
    }
}