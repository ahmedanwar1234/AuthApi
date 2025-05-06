using UserAccountAPI.Models;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace UserAccountAPI.DTOs
{
    public class DoctorDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Specialty { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsAvailableToday { get; set; }
        public int? DepartmentId { get; set; }
        public int? UserId { get; set; }  // Changed to nullable
        public ICollection<WorkingHoursDto> WorkingHours { get; set; } = new List<WorkingHoursDto>();
    }

    public class DoctorCreateDto
    {
        [Required]
        public string Name { get; set; }

        public string Specialty { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsAvailableToday { get; set; }
        public int? DepartmentId { get; set; }
        public int? UserId { get; set; }  // Changed to nullable
    }

    public class DoctorUpdateDto
    {
        public string Name { get; set; }
        public string Specialty { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsAvailableToday { get; set; }
        public int? DepartmentId { get; set; }
        public int? UserId { get; set; }  // Added UserId
    }

    public class WorkingHoursDto
    {
        public DayOfWeek DayOfWeek { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
    }
}