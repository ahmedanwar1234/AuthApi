using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserAccountAPI.DTOs;
using UserAccountAPI.Data;
using UserAccountAPI.Models;
using UserAccountAPI.Repositories.Interfaces;

namespace UserAccountAPI.Repositories
{
    // Repositories/PatientRepository.cs
    public class PatientRepository : IPatientRepository
    {
        private readonly ApplicationDbContext _context;

        public PatientRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Patient>> GetAllPatients()
        {
            return await _context.Patients.ToListAsync();
        }

        public async Task<Patient> GetPatientById(int id)
        {
            return await _context.Patients.FindAsync(id);
        }

        public async Task<Patient> GetByUserIdAsync(int userId)
        {
            return await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }

        public async Task<Patient> AddPatient(Patient patient)
        {
            patient.AdmissionDate = DateTime.Now;
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();
            return patient;
        }

        public async Task<Patient> UpdatePatient(Patient patient)
        {
            _context.Entry(patient).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return patient;
        }

        public async Task<bool> DeletePatient(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return false;

            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Patient>> GetPatientsAdmittedInYear(int year)
        {
            return await _context.Patients
                .Where(p => p.AdmissionDate.Year == year)
                .ToListAsync();
        }

        public async Task<IEnumerable<Patient>> GetPatientsByAgeRange(int minAge, int maxAge)
        {
            var today = DateTime.Today;
            return await _context.Patients
                .Where(p => (today.Year - p.DateOfBirth.Year) >= minAge &&
                            (today.Year - p.DateOfBirth.Year) <= maxAge)
                .ToListAsync();
        }

        public async Task<IEnumerable<Patient>> GetPatientsWithAppointments()
        {
            return await _context.Patients
                .Where(p => p.HasAppointments)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> CountPatientsByGender()
        {
            return await _context.Patients
                .GroupBy(p => p.Gender)
                .Select(g => new { Gender = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Gender, x => x.Count);
        }

        public async Task<IEnumerable<Patient>> SearchPatients(string searchTerm)
        {
            return await _context.Patients
                .Where(p => p.FullName.Contains(searchTerm) ||
                           p.PhoneNumber.Contains(searchTerm))
                .ToListAsync();
        }

        public async Task<Patient> UpdatePatientPhone(int id, string phoneNumber)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return null;

            patient.PhoneNumber = phoneNumber;
            await _context.SaveChangesAsync();
            return patient;
        }

        public async Task<IEnumerable<Patient>> FilterPatients(PatientFilterDTO filter)
        {
            var query = _context.Patients.AsQueryable();

            if (!string.IsNullOrEmpty(filter.Gender))
                query = query.Where(p => p.Gender == filter.Gender);

            if (filter.HasAppointments.HasValue)
                query = query.Where(p => p.HasAppointments == filter.HasAppointments);

            if (filter.MinAge.HasValue || filter.MaxAge.HasValue)
            {
                var today = DateTime.Today;
                if (filter.MinAge.HasValue)
                {
                    var minDate = today.AddYears(-filter.MinAge.Value);
                    query = query.Where(p => p.DateOfBirth <= minDate);
                }
                if (filter.MaxAge.HasValue)
                {
                    var maxDate = today.AddYears(-filter.MaxAge.Value);
                    query = query.Where(p => p.DateOfBirth >= maxDate);
                }
            }

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(p =>
                    p.FullName.Contains(filter.SearchTerm) ||
                    p.PhoneNumber.Contains(filter.SearchTerm)
                );
            }

            return await query.ToListAsync();
        }
    }
}