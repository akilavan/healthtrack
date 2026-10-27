using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace coursework_2.Models
{
    [Table("TrainerFeedback")]
    public class TrainerFeedback
    {
        public TrainerFeedback() { CreatedAt = DateTime.UtcNow; }

        [Key]
        public int FeedbackId { get; set; }

        public int TrainerId { get; set; }
        public int ClientId { get; set; }
        public int? WorkoutLogId { get; set; }

        [Required, StringLength(500)]
        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; }

        [ForeignKey("TrainerId")]
        public virtual User Trainer { get; set; }

        [ForeignKey("ClientId")]
        public virtual User Client { get; set; }

        [ForeignKey("WorkoutLogId")]
        public virtual WorkoutLog WorkoutLog { get; set; }
    }

    [Table("Notifications")]
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        public int UserId { get; set; }

        [Required, StringLength(20)]
        public string Type { get; set; }

        [Required, StringLength(120)]
        public string Title { get; set; }

        [Required, StringLength(300)]
        public string Message { get; set; }

        public DateTime ScheduledFor { get; set; }
        public DateTime? SentAt { get; set; }
        public bool IsRead { get; set; }
        public int? RelatedId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }

    [Table("AuditLogs")]
    public class AuditLog
    {
        public AuditLog() { CreatedAt = DateTime.UtcNow; }

        [Key]
        public long AuditId { get; set; }

        public int? UserId { get; set; }

        [Required, StringLength(50)]
        public string Action { get; set; }

        [StringLength(50)]
        public string EntityName { get; set; }

        public int? EntityId { get; set; }

        [StringLength(45)]
        public string IpAddress { get; set; }

        public DateTime CreatedAt { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}