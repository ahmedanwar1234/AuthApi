using AutoMapper;
using Microsoft.Extensions.Logging;
using UserAccountAPI.DTOs;
using UserAccountAPI.Models;
using UserAccountAPI.Repositories;
using UserAccountAPI.Repositories.Interfaces;

namespace UserAccountAPI.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IDoctorRepository _doctorRepository;
        private readonly IPatientRepository _patientRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<AppointmentService> _logger;

        public AppointmentService(
            IAppointmentRepository appointmentRepository,
            IDoctorRepository doctorRepository,
            IPatientRepository patientRepository,
            IMapper mapper,
            ILogger<AppointmentService> logger)
        {
            _appointmentRepository = appointmentRepository;
            _doctorRepository = doctorRepository;
            _patientRepository = patientRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<AppointmentDto>> GetAllAppointmentsAsync()
        {
            var appointments = await _appointmentRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<AppointmentDto>>(appointments);
        }

        public async Task<AppointmentDto> GetAppointmentByIdAsync(int id)
        {
            var appointment = await _appointmentRepository.GetByIdAsync(id);
            return _mapper.Map<AppointmentDto>(appointment);
        }

        public async Task<IEnumerable<AppointmentDto>> GetAppointmentsByPatientAsync(int patientId)
        {
            var appointments = await _appointmentRepository.GetByPatientAsync(patientId);
            return _mapper.Map<IEnumerable<AppointmentDto>>(appointments);
        }

        public async Task<IEnumerable<AppointmentDto>> GetAppointmentsByDoctorAsync(int doctorId)
        {
            var appointments = await _appointmentRepository.GetByDoctorAsync(doctorId);
            return _mapper.Map<IEnumerable<AppointmentDto>>(appointments);
        }

        public async Task<AppointmentDto> CreateAppointmentAsync(AppointmentCreateDto appointmentDto)
        {
            var startTime = TimeSpan.Parse(appointmentDto.StartTime);
            var endTime = TimeSpan.Parse(appointmentDto.EndTime);

            if (endTime <= startTime)
            {
                throw new InvalidOperationException("End time must be after start time.");
            }

            var isAvailable = await _appointmentRepository.IsDoctorAvailableAsync(
                appointmentDto.DoctorId,
                appointmentDto.AppointmentDate,
                startTime,
                endTime);

            if (!isAvailable)
            {
                throw new InvalidOperationException("The doctor is not available at the requested time.");
            }

            var appointment = new Appointment
            {
                PatientId = appointmentDto.PatientId,
                DoctorId = appointmentDto.DoctorId,
                AppointmentDate = appointmentDto.AppointmentDate,
                StartTime = startTime,
                EndTime = endTime,
                Status = AppointmentStatus.Scheduled,
                Notes = appointmentDto.Notes
            };

            var createdAppointment = await _appointmentRepository.AddAsync(appointment);
            return _mapper.Map<AppointmentDto>(createdAppointment);
        }

        public async Task<AppointmentDto> UpdateAppointmentAsync(int id, AppointmentUpdateDto appointmentDto)
        {
            var appointment = await _appointmentRepository.GetByIdAsync(id);
            if (appointment == null)
            {
                return null;
            }

            var startTime = TimeSpan.Parse(appointmentDto.StartTime);
            var endTime = TimeSpan.Parse(appointmentDto.EndTime);

            if (endTime <= startTime)
            {
                throw new InvalidOperationException("End time must be after start time.");
            }

            if (appointment.AppointmentDate != appointmentDto.AppointmentDate ||
                appointment.StartTime != startTime ||
                appointment.EndTime != endTime)
            {
                var isAvailable = await _appointmentRepository.IsDoctorAvailableAsync(
                    appointment.DoctorId,
                    appointmentDto.AppointmentDate,
                    startTime,
                    endTime,
                    id);

                if (!isAvailable)
                {
                    throw new InvalidOperationException("The doctor is not available at the requested time.");
                }
            }

            appointment.AppointmentDate = appointmentDto.AppointmentDate;
            appointment.StartTime = startTime;
            appointment.EndTime = endTime;
            appointment.Notes = appointmentDto.Notes;

            var updatedAppointment = await _appointmentRepository.UpdateAsync(appointment);
            return _mapper.Map<AppointmentDto>(updatedAppointment);
        }

        public async Task<bool> DeleteAppointmentAsync(int id)
        {
            return await _appointmentRepository.DeleteAsync(id);
        }

        public async Task<AppointmentDto> UpdateStatusAsync(int appointmentId, AppointmentStatusUpdateDto statusDto)
        {
            var appointment = await _appointmentRepository.GetByIdAsync(appointmentId);
            if (appointment == null)
            {
                return null;
            }

            if (!Enum.TryParse<AppointmentStatus>(statusDto.Status, true, out var newStatus))
            {
                throw new InvalidOperationException($"Invalid status value: {statusDto.Status}");
            }

            appointment.Status = newStatus;
            if (!string.IsNullOrEmpty(statusDto.Notes))
            {
                appointment.Notes = string.IsNullOrEmpty(appointment.Notes)
                    ? statusDto.Notes
                    : $"{appointment.Notes}\n{statusDto.Notes}";
            }

            var updatedAppointment = await _appointmentRepository.UpdateAsync(appointment);
            return _mapper.Map<AppointmentDto>(updatedAppointment);
        }

        public async Task<bool> CheckDoctorAvailabilityAsync(int doctorId, DateTime date, string startTime, string endTime)
        {
            return await _appointmentRepository.IsDoctorAvailableAsync(
                doctorId,
                date,
                TimeSpan.Parse(startTime),
                TimeSpan.Parse(endTime));
        }

        public async Task<IEnumerable<AppointmentDto>> FilterAppointmentsAsync(DateTime? date, int? doctorId, int? patientId, string status)
        {
            AppointmentStatus? statusEnum = null;
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<AppointmentStatus>(status, true, out var parsedStatus))
            {
                statusEnum = parsedStatus;
            }

            var appointments = await _appointmentRepository.FilterAsync(date, doctorId, patientId, statusEnum);
            return _mapper.Map<IEnumerable<AppointmentDto>>(appointments);
        }

        // Authorization check methods
        public async Task<bool> IsAppointmentForUserAsync(int appointmentId, int userId)
        {
            var appointment = await _appointmentRepository.GetByIdAsync(appointmentId);
            if (appointment == null)
            {
                return false;
            }

            var patient = await _patientRepository.GetByUserIdAsync(userId);
            return patient != null && appointment.PatientId == patient.Id;
        }

        public async Task<bool> IsAppointmentForDoctorAsync(int appointmentId, int userId)
        {
            var appointment = await _appointmentRepository.GetByIdAsync(appointmentId);
            if (appointment == null)
            {
                return false;
            }

            var doctor = await _doctorRepository.GetDoctorByUserId(userId);
            return doctor != null && appointment.DoctorId == doctor.Id;
        }

        public async Task<bool> IsPatientUserAsync(int patientId, int userId)
        {
            var patient = await _patientRepository.GetPatientById(patientId);
            return patient != null && patient.UserId == userId;
        }

        public async Task<bool> IsDoctorUserAsync(int doctorId, int userId)
        {
            var doctor = await _doctorRepository.GetDoctorById(doctorId);
            return doctor != null && doctor.UserId == userId;
        }

        public async Task<int?> GetPatientIdFromUserIdAsync(int userId)
        {
            var patient = await _patientRepository.GetByUserIdAsync(userId);
            return patient?.Id;
        }

        public async Task<int?> GetDoctorIdFromUserIdAsync(int userId)
        {
            var doctor = await _doctorRepository.GetDoctorByUserId(userId);
            return doctor?.Id;
        }
    }
}
