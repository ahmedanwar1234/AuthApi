namespace UserAccountAPI.Models
{
    public class Department
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }

        // علاقة One-to-Many مع الأطباء
        public ICollection<Doctor>? Doctors { get; set; } = new List<Doctor>();
    }
}
