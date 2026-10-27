using System;

namespace coursework_2.Models
{
    public class NamedCount
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }

    public class TrainerClientRow
    {
        public int ClientId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public int Plans { get; set; }
        public int WorkoutLogs30 { get; set; }
        public decimal CaloriesBurned30 { get; set; }
        public DateTime? LastWorkout { get; set; }
        public decimal? LatestWeight { get; set; }
        public decimal? LatestBmi { get; set; }
    }

    public class ClientPerformanceRow
    {
        public int ClientId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public int Workouts { get; set; }
        public int Minutes { get; set; }
        public decimal CaloriesOut { get; set; }
        public int Meals { get; set; }
        public decimal CaloriesIn { get; set; }
        public decimal? StartWeight { get; set; }
        public decimal? EndWeight { get; set; }
        public decimal? LatestBmi { get; set; }
        public int AppointmentsCompleted { get; set; }
        public int AppointmentsTotal { get; set; }
        public DateTime? LastActivity { get; set; }

        public decimal? WeightChange
        {
            get
            {
                if (StartWeight.HasValue && EndWeight.HasValue)
                    return EndWeight - StartWeight;
                return null;
            }
        }
    }

    public class ExerciseUsageRow
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public int Logs { get; set; }
        public int Minutes { get; set; }
        public decimal Calories { get; set; }
    }

    public class PlanUsageRow
    {
        public string Title { get; set; }
        public int Logs { get; set; }
    }

    public class EngagementRow
    {
        public int ClientId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public int Workouts { get; set; }
        public int Meals { get; set; }
        public DateTime? LastActivity { get; set; }
        public int? DaysSince { get; set; }
        public string Status { get; set; }
    }
}