using AutoMapper;
using UserAccountAPI.Models;
using UserAccountAPI.DTOs;

namespace UserAccountAPI.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // User mappings
            CreateMap<ApplicationUser, UserDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.EmailConfirmed, opt => opt.MapFrom(src => src.EmailConfirmed))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.LastLogin, opt => opt.MapFrom(src => src.LastLogin))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.NationalId, opt => opt.MapFrom(src => src.NationalId))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src =>
                    src.DateOfBirth.HasValue ? src.DateOfBirth.Value.ToString("yyyy-MM-dd") : string.Empty))
                .ForMember(dest => dest.Role, opt => opt.Ignore())
                .ForMember(dest => dest.AccessToken, opt => opt.Ignore())
                .ForMember(dest => dest.RefreshToken, opt => opt.Ignore())
                .ForMember(dest => dest.ExpiresAt, opt => opt.Ignore());

            // Patient mappings
            CreateMap<Patient, PatientDTO>();
            CreateMap<PatientUpdateDTO, Patient>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Doctor mappings
            CreateMap<Doctor, DoctorDto>()
                .ForMember(dest => dest.WorkingHours, opt => opt.MapFrom(src => src.WorkingHours));

            CreateMap<WorkingHours, WorkingHoursDto>()
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime.ToString(@"hh\:mm")))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime.ToString(@"hh\:mm")));


            // Appointment mappings
            CreateMap<AppointmentCreateDto, Appointment>();
            CreateMap<Appointment, AppointmentDto>()
                .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src =>
                    src.Patient != null ? $"{src.Patient.FullName}" : string.Empty))
                .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src =>
                    src.Doctor != null ? $"{src.Doctor.Name}" : string.Empty))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

            CreateMap<DoctorCreateDto, Doctor>();
            CreateMap<DoctorUpdateDto, Doctor>();
        }
    }
}