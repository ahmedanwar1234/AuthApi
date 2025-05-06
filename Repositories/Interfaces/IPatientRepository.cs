using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserAccountAPI.DTOs;
using UserAccountAPI.Models;

namespace UserAccountAPI.Repositories.Interfaces
{
    public interface IPatientRepository
    {
        Task<IEnumerable<Patient>> GetAllPatients();
        Task<Patient> GetPatientById(int id);
        Task<Patient> GetByUserIdAsync(int userId);  // Added this method
        Task<Patient> AddPatient(Patient patient);
        Task<Patient> UpdatePatient(Patient patient);
        Task<bool> DeletePatient(int id);
        Task<IEnumerable<Patient>> GetPatientsAdmittedInYear(int year);
        Task<IEnumerable<Patient>> GetPatientsByAgeRange(int minAge, int maxAge);
        Task<IEnumerable<Patient>> GetPatientsWithAppointments();
        Task<Dictionary<string, int>> CountPatientsByGender();
        Task<IEnumerable<Patient>> SearchPatients(string searchTerm);
        Task<Patient> UpdatePatientPhone(int id, string phoneNumber);
        Task<IEnumerable<Patient>> FilterPatients(PatientFilterDTO filter);
    }
}