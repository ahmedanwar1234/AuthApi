using UserAccountAPI.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Identity;

namespace UserAccountAPI.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<WorkingHours> WorkingHours { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);


            builder.Entity<ApplicationUser>()
             .HasOne(u => u.Patient)
             .WithOne(p => p.ApplicationUser)
             .HasForeignKey<Patient>(p => p.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Token).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
                entity.HasIndex(e => e.Token).IsUnique();

                entity.HasOne<ApplicationUser>()
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
                   builder.Entity<Doctor>()
                    .HasOne(d => d.Department)
                   .WithMany(dep => dep.Doctors)
                    .HasForeignKey(d => d.DepartmentId)
                     .OnDelete(DeleteBehavior.Restrict); 

            builder.Entity<Patient>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.FullName).IsRequired().HasMaxLength(100);
                entity.Property(p => p.Gender).IsRequired();
                entity.Property(p => p.PhoneNumber).IsRequired();

                entity.HasOne(p => p.Doctor)
                   .WithMany(d => d.Patients)
                   .HasForeignKey(p => p.DoctorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            builder.Entity<Appointment>()
               .HasOne(a => a.Patient)
               .WithMany(p => p.Appointments)
               .HasForeignKey(a => a.PatientId)
               .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Appointment>()
               .HasOne(a => a.Doctor)
               .WithMany(d => d.Appointments)  
               .HasForeignKey(a => a.DoctorId)
               .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<WorkingHours>()
                .HasOne(w => w.Doctor)
                .WithMany(d => d.WorkingHours)
                .HasForeignKey(w => w.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);


        }
    }

    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; }
        public int UserId { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsUsed { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
