using System.ComponentModel.DataAnnotations;

namespace UserAccountAPI.DTOs
{

    // DTO لعرض بيانات المريض
    public class PatientDTO
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime AdmissionDate { get; set; }
        public bool HasAppointments { get; set; }
        public int UserId { get; set; }
    }


    // DTO لتحديث بيانات المريض
    public class PatientUpdateDTO
    {
        [StringLength(100, MinimumLength = 3)]
        public string FullName { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [RegularExpression("^(Male|Female)$")]
        public string Gender { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        public string PhoneNumber { get; set; }
    }

    // DTO لفلترة المرضى
    public class PatientFilterDTO
    {
        public string Gender { get; set; }
        public bool? HasAppointments { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public string SearchTerm { get; set; }
    }
}

