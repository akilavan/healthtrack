using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace coursework_2.Models
{
    [Table("Exercises")]
    public class Exercise
    {
        public Exercise() { MetValue = 3.5m; }

        [Key]
        public int ExerciseId { get; set; }

        [Required, StringLength(100)]
        [Index("IX_Exercises_Name", IsUnique = true)]
        public string Name { get; set; }

        [Required, StringLength(30)]
        public string Category { get; set; }

        [StringLength(50)]
        public string MuscleGroup { get; set; }

        public decimal MetValue { get; set; }
    }

    [Table("WorkoutPlans")]
    public class WorkoutPlan
    {
        public WorkoutPlan()
        {
            StartDate = DateTime.Today;
            Status = "Active";
            CreatedAt = DateTime.UtcNow;
            Items = new HashSet<WorkoutPlanItem>();
        }

        [Key]
        public int PlanId { get; set; }

        public int ClientId { get; set; }
        public int TrainerId { get; set; }

        [Required, StringLength(120)]
        public string Title { get; set; }

        [StringLength(150)]
        public string Goal { get; set; }

        [Column(TypeName = "date")]
        public DateTime StartDate { get; set; }

        [Column(TypeName = "date")]
        public DateTime? EndDate { get; set; }

        [Required, StringLength(15)]
        public string Status { get; set; }

        public DateTime CreatedAt { get; set; }

        [ForeignKey("ClientId")]
        public virtual User Client { get; set; }

        [ForeignKey("TrainerId")]
        public virtual User Trainer { get; set; }

        public virtual ICollection<WorkoutPlanItem> Items { get; set; }
    }

    [Table("WorkoutPlanItems")]
    public class WorkoutPlanItem
    {
        [Key]
        public int PlanItemId { get; set; }

        public int PlanId { get; set; }
        public int ExerciseId { get; set; }

        public byte DayOfWeek { get; set; }
        public byte? Sets { get; set; }
        public short? Reps { get; set; }
        public short? DurationMin { get; set; }
        public short? RestSec { get; set; }

        [StringLength(250)]
        public string Notes { get; set; }

        [ForeignKey("PlanId")]
        public virtual WorkoutPlan Plan { get; set; }

        [ForeignKey("ExerciseId")]
        public virtual Exercise Exercise { get; set; }
    }

    [Table("WorkoutLogs")]
    public class WorkoutLog
    {
        public WorkoutLog() { CreatedAt = DateTime.UtcNow; }

        [Key]
        public int WorkoutLogId { get; set; }

        public int ClientId { get; set; }
        public int? PlanItemId { get; set; }
        public int ExerciseId { get; set; }

        [Column(TypeName = "date")]
        public DateTime LogDate { get; set; }

        public byte? Sets { get; set; }
        public short? Reps { get; set; }
        public decimal? WeightKg { get; set; }
        public short? DurationMin { get; set; }
        public decimal? CaloriesBurned { get; set; }

        [StringLength(250)]
        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; }

        [ForeignKey("ClientId")]
        public virtual User Client { get; set; }

        [ForeignKey("PlanItemId")]
        public virtual WorkoutPlanItem PlanItem { get; set; }

        [ForeignKey("ExerciseId")]
        public virtual Exercise Exercise { get; set; }
    }
}