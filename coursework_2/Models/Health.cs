using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace coursework_2.Models
{
    [Table("HealthMetrics")]
    public class HealthMetric
    {
        [Key]
        public int MetricId { get; set; }

        public int ClientId { get; set; }
        public DateTime MeasuredAt { get; set; }
        public decimal WeightKg { get; set; }
        public decimal? BMI { get; set; }
        public decimal? BodyFatPct { get; set; }
        public short? SystolicBP { get; set; }
        public short? DiastolicBP { get; set; }
        public short? RestingHR { get; set; }
        public decimal? BloodSugar { get; set; }

        [StringLength(250)]
        public string Notes { get; set; }

        [ForeignKey("ClientId")]
        public virtual User Client { get; set; }
    }

    [Table("HealthcareProviders")]
    public class HealthcareProvider
    {
        public HealthcareProvider() { IsActive = true; }

        [Key]
        public int ProviderId { get; set; }

        [Required, StringLength(120)]
        public string FullName { get; set; }

        [Required, StringLength(80)]
        public string Specialty { get; set; }

        [StringLength(120)]
        public string Clinic { get; set; }

        [StringLength(30)]
        public string Phone { get; set; }

        public bool IsActive { get; set; }
    }

    [Table("Appointments")]
    public class Appointment
    {
        public Appointment()
        {
            Status = "Pending";
            CreatedAt = DateTime.UtcNow;
        }

        [Key]
        public int AppointmentId { get; set; }

        public int ClientId { get; set; }
        public int? TrainerId { get; set; }
        public int? ProviderId { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        [StringLength(200)]
        public string Purpose { get; set; }

        [Required, StringLength(15)]
        public string Status { get; set; }

        [StringLength(250)]
        public string Remarks { get; set; }

        public DateTime CreatedAt { get; set; }

        [ForeignKey("ClientId")]
        public virtual User Client { get; set; }

        [ForeignKey("TrainerId")]
        public virtual User Trainer { get; set; }

        [ForeignKey("ProviderId")]
        public virtual HealthcareProvider Provider { get; set; }
    }
}