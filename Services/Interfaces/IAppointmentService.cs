using UserAccountAPI.DTOs;
using UserAccountAPI.Models;

namespace UserAccountAPI.Services
{
    public interface IAppointmentService
    {
        // Methods for retrieving appointments
        Task<IEnumerable<AppointmentDto>> GetAllAppointmentsAsync();
        Task<AppointmentDto> GetAppointmentByIdAsync(int id);
        Task<IEnumerable<AppointmentDto>> GetAppointmentsByPatientAsync(int patientId);
        Task<IEnumerable<AppointmentDto>> GetAppointmentsByDoctorAsync(int doctorId);

        // Methods for creating, updating, and deleting appointments
        Task<AppointmentDto> CreateAppointmentAsync(AppointmentCreateDto appointmentDto);
        Task<AppointmentDto> UpdateAppointmentAsync(int id, AppointmentUpdateDto appointmentDto);
        Task<bool> DeleteAppointmentAsync(int id);
        Task<AppointmentDto> UpdateStatusAsync(int appointmentId, AppointmentStatusUpdateDto statusDto);

        // Method to check doctor availability
        Task<bool> CheckDoctorAvailabilityAsync(int doctorId, DateTime date, string startTime, string endTime);

        // Method for filtering appointments based on various criteria
        Task<IEnumerable<AppointmentDto>> FilterAppointmentsAsync(DateTime? date, int? doctorId, int? patientId, string status);

        // Additional methods for authorization checks
        Task<bool> IsAppointmentForUserAsync(int appointmentId, int userId);
        Task<bool> IsAppointmentForDoctorAsync(int appointmentId, int userId);
        Task<bool> IsPatientUserAsync(int patientId, int userId);
        Task<bool> IsDoctorUserAsync(int doctorId, int userId);

        // Methods for getting patient and doctor IDs based on user IDs
        Task<int?> GetPatientIdFromUserIdAsync(int userId);
        Task<int?> GetDoctorIdFromUserIdAsync(int userId);
    }
}
