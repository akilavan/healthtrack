using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace coursework_2.Models
{
    public class HealthTrackContext : DbContext
    {
        public HealthTrackContext() : base("HealthTrackContext") { }

        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ClientProfile> ClientProfiles { get; set; }
        public DbSet<TrainerProfile> TrainerProfiles { get; set; }
        public DbSet<TrainerClient> TrainerClients { get; set; }
        public DbSet<Exercise> Exercises { get; set; }
        public DbSet<WorkoutPlan> WorkoutPlans { get; set; }
        public DbSet<WorkoutPlanItem> WorkoutPlanItems { get; set; }
        public DbSet<WorkoutLog> WorkoutLogs { get; set; }
        public DbSet<Food> Foods { get; set; }
        public DbSet<DietPlan> DietPlans { get; set; }
        public DbSet<DietPlanMeal> DietPlanMeals { get; set; }
        public DbSet<MealLog> MealLogs { get; set; }
        public DbSet<HealthMetric> HealthMetrics { get; set; }
        public DbSet<HealthcareProvider> HealthcareProviders { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<TrainerFeedback> TrainerFeedback { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<SmsMessage> SmsMessages { get; set; }
        public DbSet<LandingSection> LandingSections { get; set; }
        public DbSet<LandingSectionItem> LandingSectionItems { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            // Tables that point at Users more than once need explicit cascade rules,
            // otherwise SQL Server refuses the migration (multiple cascade paths).

            modelBuilder.Entity<TrainerClient>()
                .HasRequired(x => x.Trainer).WithMany().HasForeignKey(x => x.TrainerId).WillCascadeOnDelete(false);
            modelBuilder.Entity<TrainerClient>()
                .HasRequired(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).WillCascadeOnDelete(false);
            modelBuilder.Entity<TrainerClient>()
                .HasRequired(x => x.AssignedByUser).WithMany().HasForeignKey(x => x.AssignedBy).WillCascadeOnDelete(false);

            modelBuilder.Entity<WorkoutPlan>()
                .HasRequired(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).WillCascadeOnDelete(false);
            modelBuilder.Entity<WorkoutPlan>()
                .HasRequired(x => x.Trainer).WithMany().HasForeignKey(x => x.TrainerId).WillCascadeOnDelete(false);

            modelBuilder.Entity<DietPlan>()
                .HasRequired(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).WillCascadeOnDelete(false);
            modelBuilder.Entity<DietPlan>()
                .HasRequired(x => x.Trainer).WithMany().HasForeignKey(x => x.TrainerId).WillCascadeOnDelete(false);

            modelBuilder.Entity<Appointment>()
                .HasRequired(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).WillCascadeOnDelete(false);
            modelBuilder.Entity<Appointment>()
                .HasOptional(x => x.Trainer).WithMany().HasForeignKey(x => x.TrainerId).WillCascadeOnDelete(false);
            modelBuilder.Entity<Appointment>()
                .HasOptional(x => x.Provider).WithMany().HasForeignKey(x => x.ProviderId).WillCascadeOnDelete(false);

            modelBuilder.Entity<TrainerFeedback>()
                .HasRequired(x => x.Trainer).WithMany().HasForeignKey(x => x.TrainerId).WillCascadeOnDelete(false);
            modelBuilder.Entity<TrainerFeedback>()
                .HasRequired(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).WillCascadeOnDelete(false);
            modelBuilder.Entity<TrainerFeedback>()
                .HasOptional(x => x.WorkoutLog).WithMany().HasForeignKey(x => x.WorkoutLogId).WillCascadeOnDelete(false);

            modelBuilder.Entity<WorkoutLog>()
                .HasRequired(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).WillCascadeOnDelete(false);
            modelBuilder.Entity<MealLog>()
                .HasRequired(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).WillCascadeOnDelete(false);
            modelBuilder.Entity<HealthMetric>()
                .HasRequired(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).WillCascadeOnDelete(false);

            modelBuilder.Entity<WorkoutPlanItem>()
                .HasRequired(x => x.Exercise).WithMany().HasForeignKey(x => x.ExerciseId).WillCascadeOnDelete(false);
            modelBuilder.Entity<DietPlanMeal>()
                .HasRequired(x => x.Food).WithMany().HasForeignKey(x => x.FoodId).WillCascadeOnDelete(false);

            base.OnModelCreating(modelBuilder);
        }
    }
}