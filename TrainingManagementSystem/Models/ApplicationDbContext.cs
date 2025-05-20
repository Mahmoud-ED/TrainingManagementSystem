using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using TrainingManagementSystem.Models.Entities;
using TrainingManagementSystem.Models;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TrainingManagementSystem.Models
{
    //public class DataContext : DbContext
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        private readonly IServiceProvider _serviceProvider;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options
                                    , IServiceProvider serviceProvider) : base(options)
        {
            _serviceProvider = serviceProvider;
        }


        public DbSet<Contact> Contact { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseClassification> CourseClassifications { get; set; }
        public DbSet<CourseDetails> CourseDetails { get; set; }
        public DbSet<CourseTrainee> CourseTrainees { get; set; }
        public DbSet<CourseTrainer> CourseTrainers { get; set; }
        public DbSet<CourseType> CourseTypes { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Level> Levels { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Organizition> Organizitions { get; set; }
        public DbSet<Qualification> Qualifications { get; set; }
        public DbSet<PlanHeader> PlanHeaders { get; set; }
        public DbSet<Specialization> Specializations { get; set; }
        public DbSet<Status> Statuses { get; set; }
        public DbSet<Trainee> Trainees { get; set; }
        public DbSet<Trainer> Trainers { get; set; }
        public DbSet<TrainerSpecialization> TrainerSpecializations { get; set; }
        public DbSet<SiteState> SiteState { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<SiteInfo> SiteInfo { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseSqlServer("Server=.;Database=TMS;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;")
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ... (DefaultValueSql and Seeding) ...

            modelBuilder.Entity<ApplicationUser>()
                        .HasOne(a => a.UserProfile) // افترض أن ApplicationUser لديه UserProfile UserProfile { get; set; }
                        .WithOne(b => b.ApplicationUser) // افترض أن UserProfile لديه ApplicationUser ApplicationUser { get; set; }
                        .HasForeignKey<UserProfile>(b => b.UserId)
                        .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ApplicationUser>()
                        .HasOne(a => a.Employee) // افترض أن ApplicationUser لديه Employee Employee { get; set; }
                        .WithOne(b => b.ApplicationUser) // افترض أن Employee لديه ApplicationUser ApplicationUser { get; set; }
                        .HasForeignKey<Employee>(b => b.UserId)
                        .OnDelete(DeleteBehavior.Cascade);

            // Trainee -> Department
            modelBuilder.Entity<Trainee>()
                        .HasOne(t => t.Department) // <<-- اسم الخاصية المفردة المصححة في Trainee.cs
                        .WithMany(d => d.Trainees) // <<-- اسم القائمة المصححة في Department.cs
                        .HasForeignKey(t => t.DepartmentId)
                        .OnDelete(DeleteBehavior.Restrict); // مثال على تغيير سلوك الحذف
            
            
            // Trainee -> Organization (تصحيح الاسم)
            modelBuilder.Entity<Trainee>()
                        .HasOne(t => t.Organization) // <<-- Organaizations -> Organization
                        .WithMany(o => o.Trainees)
                        .HasForeignKey(t => t.OrganizationId) // <<-- OrganaizitionId -> OrganizationId
                        .OnDelete(DeleteBehavior.Restrict);

            // Trainee -> Specialization
            modelBuilder.Entity<Trainee>()
                        .HasOne(t => t.Specialization) // <<-- Specializations -> Specialization
                        .WithMany(s => s.Trainees)
                        .HasForeignKey(t => t.SpecializationId)
                        .OnDelete(DeleteBehavior.Restrict);

            // Trainee -> Qualification
            modelBuilder.Entity<Trainee>()
                        .HasOne(t => t.Qualification) // <<-- qualifications -> Qualification
                        .WithMany(q => q.Trainees)
                        .HasForeignKey(t => t.QualificationId)
                        .OnDelete(DeleteBehavior.Restrict);

            // **تصحيح العلاقة بين Trainee و CourseTrainee (One-to-Many)**
            modelBuilder.Entity<CourseTrainee>()
                        .HasOne(ct => ct.Trainee)
                        .WithMany(t => t.CourseTrainees) // Trainee.CourseTrainees هو List
                        .HasForeignKey(ct => ct.TraineeId)
                        .OnDelete(DeleteBehavior.Cascade); // إذا حذفت متدرب، احذف تسجيلاته

            // **تصحيح العلاقة بين CourseDetails و CourseTrainee (One-to-Many)**
            modelBuilder.Entity<CourseTrainee>()
                        .HasOne(ct => ct.CourseDetails)
                        .WithMany(cd => cd.CourseTrainees) // CourseDetails.CourseTrainees هو List
                        .HasForeignKey(ct => ct.CourseDetailsId)
                        .OnDelete(DeleteBehavior.Cascade); // إذا حذفت تفاصيل دورة، احذف تسجيلات المتدربين فيها


            // Trainer -> Qualification
            modelBuilder.Entity<Trainer>()
                        .HasOne(t => t.Qualification) // <<-- Qualifications -> Qualification
                        .WithMany(q => q.Trainers)
                        .HasForeignKey(t => t.QualificationId)
                        .OnDelete(DeleteBehavior.Restrict);

            // TrainerSpecialization -> Trainer
            modelBuilder.Entity<TrainerSpecialization>()
                        .HasOne(ts => ts.Trainer)
                        .WithMany(t => t.TrainerSpecializations)
                        .HasForeignKey(ts => ts.TrainerId)
                        .OnDelete(DeleteBehavior.Cascade);


            // TrainerSpecialization -> Specialization
            modelBuilder.Entity<TrainerSpecialization>()
                        .HasOne(ts => ts.Specialization)
                        .WithMany(s => s.TrainerSpecializations)
                        .HasForeignKey(ts => ts.SpecializationId)
                        .OnDelete(DeleteBehavior.Cascade);


            // Course -> Level
            modelBuilder.Entity<Course>()
                        .HasOne(c => c.Level) // <<-- Levels -> Level
                        .WithMany(l => l.Courses)
                        .HasForeignKey(c => c.LevelId)
                        .OnDelete(DeleteBehavior.Restrict);

            // Course -> CourseClassification
            modelBuilder.Entity<Course>()
                        .HasOne(c => c.CourseClassification) // <<-- CourseClassifications -> CourseClassification
                        .WithMany(cc => cc.Courses)
                        .HasForeignKey(c => c.CourseClassificationId)
                        .OnDelete(DeleteBehavior.Restrict);

            // CourseDetails -> Course
            modelBuilder.Entity<CourseDetails>()
                        .HasOne(cd => cd.Course) // <<-- Courses -> Course
                        .WithMany(c => c.CourseDetails)
                        .HasForeignKey(cd => cd.CourseId)
                        .OnDelete(DeleteBehavior.Cascade); // إذا حذفت دورة، احذف تفاصيلها

            // CourseDetails -> Location
            modelBuilder.Entity<CourseDetails>()
                        .HasOne(cd => cd.Location) // <<-- Locations -> Location
                        .WithMany(l => l.CourseDetails)
                        .HasForeignKey(cd => cd.LocationId)
                        .OnDelete(DeleteBehavior.Restrict);

            // CourseDetails -> CourseType
            modelBuilder.Entity<CourseDetails>()
                        .HasOne(cd => cd.CourseType) // <<-- CourseTypes -> CourseType
                        .WithMany(ct => ct.CourseDetails)
                        .HasForeignKey(cd => cd.CourseTypeId)
                        .OnDelete(DeleteBehavior.Restrict);

            // CourseDetails -> Status
            modelBuilder.Entity<CourseDetails>()
                        .HasOne(cd => cd.Status) // <<-- Statuses -> Status
                        .WithMany(s => s.CourseDetails)
                        .HasForeignKey(cd => cd.StatusId)
                        .OnDelete(DeleteBehavior.Restrict);

            //// **العلاقات الخاصة بـ CourseTrainer (Many-to-Many بين CourseDetails و Trainer)**
            //modelBuilder.Entity<CourseTrainer>()
            //            .HasOne(ct => ct.CourseDetails)
            //            .WithMany(cd => cd.CourseTrainers) // CourseDetails.CourseTrainers هو List<CourseTrainer>
            //            .HasForeignKey(ct => ct.CourseDetailsId)
            //            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CourseTrainer>()
                        .HasOne(ct => ct.Trainer) // Trainers -> Trainer
                        .WithMany(t => t.CourseTrainers) // Trainer.CourseTrainers هو List<CourseTrainer>
                        .HasForeignKey(ct => ct.TrainerId)
                        .OnDelete(DeleteBehavior.Cascade);

            // Organization -> Category (تصحيح الاسم)
            modelBuilder.Entity<Organizition>() // <<-- Organizition -> Organization
                        .HasOne(o => o.Category) // <<-- Categories -> Category
                        .WithMany(c => c.Organizations) // <<-- Organizitions -> Organizations
                        .HasForeignKey(o => o.CategoryId)
                        .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Trainer>()
                        .HasOne(t => t.ApplicationUser)
                        .WithOne(u => u.Trainer)
                        .HasForeignKey<Trainer>(t => t.UserId)
                        .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CourseTrainer>()
                        .HasOne(ct => ct.Course)
                        .WithMany(c => c.CourseTrainers)
                        .HasForeignKey(ct => ct.CourseId)
                        .OnDelete(DeleteBehavior.Cascade);


        }

    }
}