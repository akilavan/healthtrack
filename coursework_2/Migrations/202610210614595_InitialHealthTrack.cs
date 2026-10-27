namespace coursework_2.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialHealthTrack : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Appointments",
                c => new
                    {
                        AppointmentId = c.Int(nullable: false, identity: true),
                        ClientId = c.Int(nullable: false),
                        TrainerId = c.Int(),
                        ProviderId = c.Int(),
                        StartTime = c.DateTime(nullable: false),
                        EndTime = c.DateTime(nullable: false),
                        Purpose = c.String(maxLength: 200),
                        Status = c.String(nullable: false, maxLength: 15),
                        Remarks = c.String(maxLength: 250),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.AppointmentId)
                .ForeignKey("dbo.Users", t => t.ClientId)
                .ForeignKey("dbo.HealthcareProviders", t => t.ProviderId)
                .ForeignKey("dbo.Users", t => t.TrainerId)
                .Index(t => t.ClientId)
                .Index(t => t.TrainerId)
                .Index(t => t.ProviderId);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        UserId = c.Int(nullable: false, identity: true),
                        RoleId = c.Int(nullable: false),
                        Email = c.String(nullable: false, maxLength: 150),
                        PasswordHash = c.String(nullable: false, maxLength: 255),
                        FullName = c.String(nullable: false, maxLength: 120),
                        Phone = c.String(maxLength: 30),
                        DateOfBirth = c.DateTime(storeType: "date"),
                        Gender = c.String(maxLength: 10),
                        IsActive = c.Boolean(nullable: false),
                        FailedLogins = c.Int(nullable: false),
                        LockoutEnd = c.DateTime(),
                        CreatedAt = c.DateTime(nullable: false),
                        LastLoginAt = c.DateTime(),
                    })
                .PrimaryKey(t => t.UserId)
                .ForeignKey("dbo.Roles", t => t.RoleId, cascadeDelete: true)
                .Index(t => t.RoleId)
                .Index(t => t.Email, unique: true, name: "IX_Users_Email");
            
            CreateTable(
                "dbo.ClientProfiles",
                c => new
                    {
                        UserId = c.Int(nullable: false),
                        HeightCm = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GoalWeightKg = c.Decimal(precision: 18, scale: 2),
                        FitnessGoal = c.String(maxLength: 100),
                        ActivityLevel = c.String(maxLength: 20),
                        MedicalNotes = c.String(maxLength: 500),
                    })
                .PrimaryKey(t => t.UserId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.Roles",
                c => new
                    {
                        RoleId = c.Int(nullable: false, identity: true),
                        RoleName = c.String(nullable: false, maxLength: 30),
                    })
                .PrimaryKey(t => t.RoleId)
                .Index(t => t.RoleName, unique: true, name: "IX_Roles_RoleName");
            
            CreateTable(
                "dbo.TrainerProfiles",
                c => new
                    {
                        UserId = c.Int(nullable: false),
                        Specialization = c.String(nullable: false, maxLength: 100),
                        Certification = c.String(maxLength: 150),
                        YearsExperience = c.Int(),
                        Bio = c.String(maxLength: 500),
                    })
                .PrimaryKey(t => t.UserId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.HealthcareProviders",
                c => new
                    {
                        ProviderId = c.Int(nullable: false, identity: true),
                        FullName = c.String(nullable: false, maxLength: 120),
                        Specialty = c.String(nullable: false, maxLength: 80),
                        Clinic = c.String(maxLength: 120),
                        Phone = c.String(maxLength: 30),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.ProviderId);
            
            CreateTable(
                "dbo.AuditLogs",
                c => new
                    {
                        AuditId = c.Long(nullable: false, identity: true),
                        UserId = c.Int(),
                        Action = c.String(nullable: false, maxLength: 50),
                        EntityName = c.String(maxLength: 50),
                        EntityId = c.Int(),
                        IpAddress = c.String(maxLength: 45),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.AuditId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.DietPlanMeals",
                c => new
                    {
                        DietPlanMealId = c.Int(nullable: false, identity: true),
                        DietPlanId = c.Int(nullable: false),
                        FoodId = c.Int(nullable: false),
                        MealType = c.String(nullable: false, maxLength: 15),
                        DayOfWeek = c.Byte(nullable: false),
                        Quantity = c.Decimal(nullable: false, precision: 18, scale: 2),
                        MealTime = c.Time(precision: 7),
                    })
                .PrimaryKey(t => t.DietPlanMealId)
                .ForeignKey("dbo.DietPlans", t => t.DietPlanId, cascadeDelete: true)
                .ForeignKey("dbo.Foods", t => t.FoodId)
                .Index(t => t.DietPlanId)
                .Index(t => t.FoodId);
            
            CreateTable(
                "dbo.DietPlans",
                c => new
                    {
                        DietPlanId = c.Int(nullable: false, identity: true),
                        ClientId = c.Int(nullable: false),
                        TrainerId = c.Int(nullable: false),
                        Title = c.String(nullable: false, maxLength: 120),
                        DailyCalorieTarget = c.Int(nullable: false),
                        StartDate = c.DateTime(nullable: false, storeType: "date"),
                        EndDate = c.DateTime(storeType: "date"),
                        Status = c.String(nullable: false, maxLength: 15),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.DietPlanId)
                .ForeignKey("dbo.Users", t => t.ClientId)
                .ForeignKey("dbo.Users", t => t.TrainerId)
                .Index(t => t.ClientId)
                .Index(t => t.TrainerId);
            
            CreateTable(
                "dbo.Foods",
                c => new
                    {
                        FoodId = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 120),
                        ServingDesc = c.String(nullable: false, maxLength: 50),
                        Calories = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ProteinG = c.Decimal(nullable: false, precision: 18, scale: 2),
                        CarbsG = c.Decimal(nullable: false, precision: 18, scale: 2),
                        FatG = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.FoodId)
                .Index(t => t.Name, unique: true, name: "IX_Foods_Name");
            
            CreateTable(
                "dbo.Exercises",
                c => new
                    {
                        ExerciseId = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                        Category = c.String(nullable: false, maxLength: 30),
                        MuscleGroup = c.String(maxLength: 50),
                        CaloriesPerMinute = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.ExerciseId)
                .Index(t => t.Name, unique: true, name: "IX_Exercises_Name");
            
            CreateTable(
                "dbo.HealthMetrics",
                c => new
                    {
                        MetricId = c.Int(nullable: false, identity: true),
                        ClientId = c.Int(nullable: false),
                        MeasuredAt = c.DateTime(nullable: false),
                        WeightKg = c.Decimal(nullable: false, precision: 18, scale: 2),
                        BMI = c.Decimal(precision: 18, scale: 2),
                        BodyFatPct = c.Decimal(precision: 18, scale: 2),
                        SystolicBP = c.Short(),
                        DiastolicBP = c.Short(),
                        RestingHR = c.Short(),
                        BloodSugar = c.Decimal(precision: 18, scale: 2),
                        Notes = c.String(maxLength: 250),
                    })
                .PrimaryKey(t => t.MetricId)
                .ForeignKey("dbo.Users", t => t.ClientId)
                .Index(t => t.ClientId);
            
            CreateTable(
                "dbo.MealLogs",
                c => new
                    {
                        MealLogId = c.Int(nullable: false, identity: true),
                        ClientId = c.Int(nullable: false),
                        FoodId = c.Int(nullable: false),
                        MealType = c.String(nullable: false, maxLength: 15),
                        LogDate = c.DateTime(nullable: false, storeType: "date"),
                        Quantity = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Calories = c.Decimal(nullable: false, precision: 18, scale: 2),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.MealLogId)
                .ForeignKey("dbo.Users", t => t.ClientId)
                .ForeignKey("dbo.Foods", t => t.FoodId, cascadeDelete: true)
                .Index(t => t.ClientId)
                .Index(t => t.FoodId);
            
            CreateTable(
                "dbo.Notifications",
                c => new
                    {
                        NotificationId = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        Type = c.String(nullable: false, maxLength: 20),
                        Title = c.String(nullable: false, maxLength: 120),
                        Message = c.String(nullable: false, maxLength: 300),
                        ScheduledFor = c.DateTime(nullable: false),
                        SentAt = c.DateTime(),
                        IsRead = c.Boolean(nullable: false),
                        RelatedId = c.Int(),
                    })
                .PrimaryKey(t => t.NotificationId)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.TrainerClients",
                c => new
                    {
                        AssignmentId = c.Int(nullable: false, identity: true),
                        TrainerId = c.Int(nullable: false),
                        ClientId = c.Int(nullable: false),
                        AssignedBy = c.Int(nullable: false),
                        AssignedAt = c.DateTime(nullable: false),
                        Status = c.String(nullable: false, maxLength: 15),
                    })
                .PrimaryKey(t => t.AssignmentId)
                .ForeignKey("dbo.Users", t => t.AssignedBy)
                .ForeignKey("dbo.Users", t => t.ClientId)
                .ForeignKey("dbo.Users", t => t.TrainerId)
                .Index(t => new { t.TrainerId, t.ClientId }, unique: true, name: "UQ_TrainerClient")
                .Index(t => t.AssignedBy);
            
            CreateTable(
                "dbo.TrainerFeedback",
                c => new
                    {
                        FeedbackId = c.Int(nullable: false, identity: true),
                        TrainerId = c.Int(nullable: false),
                        ClientId = c.Int(nullable: false),
                        WorkoutLogId = c.Int(),
                        Comment = c.String(nullable: false, maxLength: 500),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.FeedbackId)
                .ForeignKey("dbo.Users", t => t.ClientId)
                .ForeignKey("dbo.Users", t => t.TrainerId)
                .ForeignKey("dbo.WorkoutLogs", t => t.WorkoutLogId)
                .Index(t => t.TrainerId)
                .Index(t => t.ClientId)
                .Index(t => t.WorkoutLogId);
            
            CreateTable(
                "dbo.WorkoutLogs",
                c => new
                    {
                        WorkoutLogId = c.Int(nullable: false, identity: true),
                        ClientId = c.Int(nullable: false),
                        PlanItemId = c.Int(),
                        ExerciseId = c.Int(nullable: false),
                        LogDate = c.DateTime(nullable: false, storeType: "date"),
                        Sets = c.Byte(),
                        Reps = c.Short(),
                        WeightKg = c.Decimal(precision: 18, scale: 2),
                        DurationMin = c.Short(),
                        CaloriesBurned = c.Decimal(precision: 18, scale: 2),
                        Notes = c.String(maxLength: 250),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.WorkoutLogId)
                .ForeignKey("dbo.Users", t => t.ClientId)
                .ForeignKey("dbo.Exercises", t => t.ExerciseId, cascadeDelete: true)
                .ForeignKey("dbo.WorkoutPlanItems", t => t.PlanItemId)
                .Index(t => t.ClientId)
                .Index(t => t.PlanItemId)
                .Index(t => t.ExerciseId);
            
            CreateTable(
                "dbo.WorkoutPlanItems",
                c => new
                    {
                        PlanItemId = c.Int(nullable: false, identity: true),
                        PlanId = c.Int(nullable: false),
                        ExerciseId = c.Int(nullable: false),
                        DayOfWeek = c.Byte(nullable: false),
                        Sets = c.Byte(),
                        Reps = c.Short(),
                        DurationMin = c.Short(),
                        RestSec = c.Short(),
                        Notes = c.String(maxLength: 250),
                    })
                .PrimaryKey(t => t.PlanItemId)
                .ForeignKey("dbo.Exercises", t => t.ExerciseId)
                .ForeignKey("dbo.WorkoutPlans", t => t.PlanId, cascadeDelete: true)
                .Index(t => t.PlanId)
                .Index(t => t.ExerciseId);
            
            CreateTable(
                "dbo.WorkoutPlans",
                c => new
                    {
                        PlanId = c.Int(nullable: false, identity: true),
                        ClientId = c.Int(nullable: false),
                        TrainerId = c.Int(nullable: false),
                        Title = c.String(nullable: false, maxLength: 120),
                        Goal = c.String(maxLength: 150),
                        StartDate = c.DateTime(nullable: false, storeType: "date"),
                        EndDate = c.DateTime(storeType: "date"),
                        Status = c.String(nullable: false, maxLength: 15),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.PlanId)
                .ForeignKey("dbo.Users", t => t.ClientId)
                .ForeignKey("dbo.Users", t => t.TrainerId)
                .Index(t => t.ClientId)
                .Index(t => t.TrainerId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.TrainerFeedback", "WorkoutLogId", "dbo.WorkoutLogs");
            DropForeignKey("dbo.WorkoutLogs", "PlanItemId", "dbo.WorkoutPlanItems");
            DropForeignKey("dbo.WorkoutPlans", "TrainerId", "dbo.Users");
            DropForeignKey("dbo.WorkoutPlanItems", "PlanId", "dbo.WorkoutPlans");
            DropForeignKey("dbo.WorkoutPlans", "ClientId", "dbo.Users");
            DropForeignKey("dbo.WorkoutPlanItems", "ExerciseId", "dbo.Exercises");
            DropForeignKey("dbo.WorkoutLogs", "ExerciseId", "dbo.Exercises");
            DropForeignKey("dbo.WorkoutLogs", "ClientId", "dbo.Users");
            DropForeignKey("dbo.TrainerFeedback", "TrainerId", "dbo.Users");
            DropForeignKey("dbo.TrainerFeedback", "ClientId", "dbo.Users");
            DropForeignKey("dbo.TrainerClients", "TrainerId", "dbo.Users");
            DropForeignKey("dbo.TrainerClients", "ClientId", "dbo.Users");
            DropForeignKey("dbo.TrainerClients", "AssignedBy", "dbo.Users");
            DropForeignKey("dbo.Notifications", "UserId", "dbo.Users");
            DropForeignKey("dbo.MealLogs", "FoodId", "dbo.Foods");
            DropForeignKey("dbo.MealLogs", "ClientId", "dbo.Users");
            DropForeignKey("dbo.HealthMetrics", "ClientId", "dbo.Users");
            DropForeignKey("dbo.DietPlanMeals", "FoodId", "dbo.Foods");
            DropForeignKey("dbo.DietPlans", "TrainerId", "dbo.Users");
            DropForeignKey("dbo.DietPlanMeals", "DietPlanId", "dbo.DietPlans");
            DropForeignKey("dbo.DietPlans", "ClientId", "dbo.Users");
            DropForeignKey("dbo.AuditLogs", "UserId", "dbo.Users");
            DropForeignKey("dbo.Appointments", "TrainerId", "dbo.Users");
            DropForeignKey("dbo.Appointments", "ProviderId", "dbo.HealthcareProviders");
            DropForeignKey("dbo.Appointments", "ClientId", "dbo.Users");
            DropForeignKey("dbo.TrainerProfiles", "UserId", "dbo.Users");
            DropForeignKey("dbo.Users", "RoleId", "dbo.Roles");
            DropForeignKey("dbo.ClientProfiles", "UserId", "dbo.Users");
            DropIndex("dbo.WorkoutPlans", new[] { "TrainerId" });
            DropIndex("dbo.WorkoutPlans", new[] { "ClientId" });
            DropIndex("dbo.WorkoutPlanItems", new[] { "ExerciseId" });
            DropIndex("dbo.WorkoutPlanItems", new[] { "PlanId" });
            DropIndex("dbo.WorkoutLogs", new[] { "ExerciseId" });
            DropIndex("dbo.WorkoutLogs", new[] { "PlanItemId" });
            DropIndex("dbo.WorkoutLogs", new[] { "ClientId" });
            DropIndex("dbo.TrainerFeedback", new[] { "WorkoutLogId" });
            DropIndex("dbo.TrainerFeedback", new[] { "ClientId" });
            DropIndex("dbo.TrainerFeedback", new[] { "TrainerId" });
            DropIndex("dbo.TrainerClients", new[] { "AssignedBy" });
            DropIndex("dbo.TrainerClients", "UQ_TrainerClient");
            DropIndex("dbo.Notifications", new[] { "UserId" });
            DropIndex("dbo.MealLogs", new[] { "FoodId" });
            DropIndex("dbo.MealLogs", new[] { "ClientId" });
            DropIndex("dbo.HealthMetrics", new[] { "ClientId" });
            DropIndex("dbo.Exercises", "IX_Exercises_Name");
            DropIndex("dbo.Foods", "IX_Foods_Name");
            DropIndex("dbo.DietPlans", new[] { "TrainerId" });
            DropIndex("dbo.DietPlans", new[] { "ClientId" });
            DropIndex("dbo.DietPlanMeals", new[] { "FoodId" });
            DropIndex("dbo.DietPlanMeals", new[] { "DietPlanId" });
            DropIndex("dbo.AuditLogs", new[] { "UserId" });
            DropIndex("dbo.TrainerProfiles", new[] { "UserId" });
            DropIndex("dbo.Roles", "IX_Roles_RoleName");
            DropIndex("dbo.ClientProfiles", new[] { "UserId" });
            DropIndex("dbo.Users", "IX_Users_Email");
            DropIndex("dbo.Users", new[] { "RoleId" });
            DropIndex("dbo.Appointments", new[] { "ProviderId" });
            DropIndex("dbo.Appointments", new[] { "TrainerId" });
            DropIndex("dbo.Appointments", new[] { "ClientId" });
            DropTable("dbo.WorkoutPlans");
            DropTable("dbo.WorkoutPlanItems");
            DropTable("dbo.WorkoutLogs");
            DropTable("dbo.TrainerFeedback");
            DropTable("dbo.TrainerClients");
            DropTable("dbo.Notifications");
            DropTable("dbo.MealLogs");
            DropTable("dbo.HealthMetrics");
            DropTable("dbo.Exercises");
            DropTable("dbo.Foods");
            DropTable("dbo.DietPlans");
            DropTable("dbo.DietPlanMeals");
            DropTable("dbo.AuditLogs");
            DropTable("dbo.HealthcareProviders");
            DropTable("dbo.TrainerProfiles");
            DropTable("dbo.Roles");
            DropTable("dbo.ClientProfiles");
            DropTable("dbo.Users");
            DropTable("dbo.Appointments");
        }
    }
}
