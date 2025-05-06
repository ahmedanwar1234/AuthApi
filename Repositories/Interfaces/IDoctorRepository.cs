// IDoctorRepository.cs
using UserAccountAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UserAccountAPI.Repositories
{
    public interface IDoctorRepository
    {
        Task<IEnumerable<Doctor>> GetAllDoctors();
        Task<Doctor> GetDoctorById(int id);
        Task<Doctor> GetDoctorByUserId(int? userId);
        Task<Doctor> AddDoctor(Doctor doctor);
        Task<Doctor> UpdateDoctor(Doctor doctor);
        Task<bool> DeleteDoctor(int id);
        Task<IEnumerable<Doctor>> GetDoctorsByDepartment(int departmentId);
        Task<IEnumerable<Doctor>> GetDoctorsBySpecialty(string specialty);
        Task<IEnumerable<Doctor>> GetAvailableDoctorsToday();
        Task<Doctor> UpdateWorkingHours(int doctorId, List<WorkingHours> workingHours);
        Task<Doctor> AssignDepartment(int doctorId, int departmentId);
        Task<IEnumerable<Doctor>> FilterDoctors(string specialty, bool? available);
        Task<Doctor> LinkDoctorToUser(int doctorId, int userId);
    }
}