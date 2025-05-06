using UserAccountAPI.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserAccountAPI.Models
{
    public class Patient
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime AdmissionDate { get; set; }
        public bool HasAppointments { get; set; }

        // Relationship with User
        public int UserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        // Relationship with Doctor
        public int? DoctorId { get; set; }  // Optional foreign key to doctor
        public Doctor Doctor { get; set; }

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
