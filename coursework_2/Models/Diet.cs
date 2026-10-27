using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace coursework_2.Models
{
    [Table("Foods")]
    public class Food
    {
        public Food() { ProteinG = 0; CarbsG = 0; FatG = 0; }

        [Key]
        public int FoodId { get; set; }

        [Required, StringLength(120)]
        [Index("IX_Foods_Name", IsUnique = true)]
        public string Name { get; set; }

        [Required, StringLength(50)]
        public string ServingDesc { get; set; }

        public decimal Calories { get; set; }
        public decimal ProteinG { get; set; }
        public decimal CarbsG { get; set; }
        public decimal FatG { get; set; }
    }

    [Table("DietPlans")]
    public class DietPlan
    {
        public DietPlan()
        {
            StartDate = DateTime.Today;
            Status = "Active";
            CreatedAt = DateTime.UtcNow;
            Meals = new HashSet<DietPlanMeal>();
        }

        [Key]
        public int DietPlanId { get; set; }

        public int ClientId { get; set; }
        public int TrainerId { get; set; }

        [Required, StringLength(120)]
        public string Title { get; set; }

        public int DailyCalorieTarget { get; set; }

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

        public virtual ICollection<DietPlanMeal> Meals { get; set; }
    }

    [Table("DietPlanMeals")]
    public class DietPlanMeal
    {
        [Key]
        public int DietPlanMealId { get; set; }

        public int DietPlanId { get; set; }
        public int FoodId { get; set; }

        [Required, StringLength(15)]
        public string MealType { get; set; }

        public byte DayOfWeek { get; set; }
        public decimal Quantity { get; set; } = 1m;
        public TimeSpan? MealTime { get; set; }

        [ForeignKey("DietPlanId")]
        public virtual DietPlan DietPlan { get; set; }

        [ForeignKey("FoodId")]
        public virtual Food Food { get; set; }
    }

    [Table("MealLogs")]
    public class MealLog
    {
        public MealLog()
        {
            LogDate = DateTime.Today;
            Quantity = 1m;
            CreatedAt = DateTime.UtcNow;
        }

        [Key]
        public int MealLogId { get; set; }

        public int ClientId { get; set; }
        public int FoodId { get; set; }

        [Required, StringLength(15)]
        public string MealType { get; set; }

        [Column(TypeName = "date")]
        public DateTime LogDate { get; set; }

        public decimal Quantity { get; set; }
        public decimal Calories { get; set; }
        public DateTime CreatedAt { get; set; }

        [ForeignKey("ClientId")]
        public virtual User Client { get; set; }

        [ForeignKey("FoodId")]
        public virtual Food Food { get; set; }
    }
}