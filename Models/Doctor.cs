using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UserAccountAPI.Models;

namespace UserAccountAPI.Models
{
    public class Doctor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(100)]
        public string Specialty { get; set; }

        [StringLength(20)]
        public string PhoneNumber { get; set; }

        public bool IsAvailableToday { get; set; } = false;

        public int? DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }

        // Make UserId nullable to allow SQL insertion without a linked user
        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser ApplicationUser { get; set; }

        public virtual ICollection<WorkingHours> WorkingHours { get; set; } = new List<WorkingHours>();
        public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }

    public class WorkingHours
    {
        [Key]
        public int Id { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public int DoctorId { get; set; }
        [ForeignKey("DoctorId")]
        public virtual Doctor Doctor { get; set; }
    }
}