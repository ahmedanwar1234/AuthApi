// DoctorRepository.cs
using UserAccountAPI.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserAccountAPI.Data;

namespace UserAccountAPI.Repositories
{
    public class DoctorRepository : IDoctorRepository
    {
        private readonly ApplicationDbContext _context;

        public DoctorRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Doctor>> GetAllDoctors()
        {
            return await _context.Doctors
                .Include(d => d.Department)
                .Include(d => d.WorkingHours)
                .ToListAsync();
        }

        public async Task<Doctor> GetDoctorById(int id)
        {
            return await _context.Doctors
                .Include(d => d.Department)
                .Include(d => d.WorkingHours)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<Doctor> GetDoctorByUserId(int? userId)
        {
            if (!userId.HasValue)
                return null;

            return await _context.Doctors
                .Include(d => d.Department)
                .Include(d => d.WorkingHours)
                .FirstOrDefaultAsync(d => d.UserId == userId);
        }

        public async Task<Doctor> AddDoctor(Doctor doctor)
        {
            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();
            return doctor;
        }

        public async Task<Doctor> UpdateDoctor(Doctor doctor)
        {
            _context.Entry(doctor).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return doctor;
        }

        // New method to link a doctor created via SQL to a user account
        public async Task<Doctor> LinkDoctorToUser(int doctorId, int userId)
        {
            var doctor = await _context.Doctors.FindAsync(doctorId);
            if (doctor == null) return null;

            // Check if user exists
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            // Check if doctor is already linked to this or another user
            if (doctor.UserId.HasValue)
            {
                if (doctor.UserId == userId)
                    return doctor; // Already linked to this user
                else
                    return null; // Linked to different user, cannot change
            }

            doctor.UserId = userId;
            await _context.SaveChangesAsync();
            return doctor;
        }

        public async Task<bool> DeleteDoctor(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null) return false;

            _context.Doctors.Remove(doctor);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Doctor>> GetDoctorsByDepartment(int departmentId)
        {
            return await _context.Doctors
                .Include(d => d.Department)
                .Include(d => d.WorkingHours)
                .Where(d => d.DepartmentId == departmentId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Doctor>> GetDoctorsBySpecialty(string specialty)
        {
            return await _context.Doctors
                .Include(d => d.Department)
                .Include(d => d.WorkingHours)
                .Where(d => d.Specialty.ToLower() == specialty.ToLower())
                .ToListAsync();
        }

        public async Task<IEnumerable<Doctor>> GetAvailableDoctorsToday()
        {
            return await _context.Doctors
                .Include(d => d.Department)
                .Include(d => d.WorkingHours)
                .Where(d => d.IsAvailableToday)
                .ToListAsync();
        }

        public async Task<Doctor> UpdateWorkingHours(int doctorId, List<WorkingHours> workingHours)
        {
            var doctor = await _context.Doctors
                .Include(d => d.WorkingHours)
                .FirstOrDefaultAsync(d => d.Id == doctorId);

            if (doctor == null) return null;

            if (doctor.WorkingHours == null)
            {
                doctor.WorkingHours = new List<WorkingHours>(workingHours);
            }
            else
            {
                foreach (var newHours in workingHours)
                {
                    var existingHours = doctor.WorkingHours
                        .FirstOrDefault(wh => wh.DayOfWeek == newHours.DayOfWeek);

                    if (existingHours != null)
                    {
                        existingHours.StartTime = newHours.StartTime;
                        existingHours.EndTime = newHours.EndTime;
                    }
                    else
                    {
                        doctor.WorkingHours.Add(newHours);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return doctor;
        }

        public async Task<Doctor> AssignDepartment(int doctorId, int departmentId)
        {
            var doctor = await _context.Doctors.FindAsync(doctorId);
            if (doctor == null) return null;

            doctor.DepartmentId = departmentId;
            await _context.SaveChangesAsync();
            return doctor;
        }

        public async Task<IEnumerable<Doctor>> FilterDoctors(string specialty, bool? available)
        {
            var query = _context.Doctors
                .Include(d => d.Department)
                .Include(d => d.WorkingHours)
                .AsQueryable();

            if (!string.IsNullOrEmpty(specialty))
                query = query.Where(d => d.Specialty.Contains(specialty));

            if (available.HasValue)
                query = query.Where(d => d.IsAvailableToday == available);

            return await query.ToListAsync();
        }
    }
}