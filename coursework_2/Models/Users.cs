using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace coursework_2.Models
{
    [Table("Roles")]
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        [Required, StringLength(30)]
        [Index("IX_Roles_RoleName", IsUnique = true)]
        public string RoleName { get; set; }

        public virtual ICollection<User> Users { get; set; }
    }

    [Table("Users")]
    public class User
    {
        public User()
        {
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        [Key]
        public int UserId { get; set; }

        [Required]
        public int RoleId { get; set; }

        [Required, StringLength(150)]
        [Index("IX_Users_Email", IsUnique = true)]
        public string Email { get; set; }

        [Required, StringLength(255)]
        public string PasswordHash { get; set; }

        [Required, StringLength(120)]
        public string FullName { get; set; }

        [StringLength(30)]
        public string Phone { get; set; }

        public bool PhoneVerified { get; set; }

        [StringLength(10)]
        public string PhoneVerificationCode { get; set; }

        public DateTime? PhoneVerificationSentAt { get; set; }

        public DateTime? PhoneVerificationExpiresAt { get; set; }

        public int PhoneVerificationAttempts { get; set; }

        [StringLength(300)]
        public string PhotoPath { get; set; }

        [Column(TypeName = "date")]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        public string Gender { get; set; }

        public bool IsActive { get; set; }
        public int FailedLogins { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; }

        public virtual ClientProfile ClientProfile { get; set; }
        public virtual TrainerProfile TrainerProfile { get; set; }
    }

    [Table("ClientProfiles")]
    public class ClientProfile
    {
        [Key, ForeignKey("User")]
        public int UserId { get; set; }

        public decimal HeightCm { get; set; }
        public decimal? GoalWeightKg { get; set; }

        [StringLength(100)]
        public string FitnessGoal { get; set; }

        [StringLength(20)]
        public string ActivityLevel { get; set; }

        [StringLength(500)]
        public string MedicalNotes { get; set; }

        public virtual User User { get; set; }
    }

    [Table("TrainerProfiles")]
    public class TrainerProfile
    {
        [Key, ForeignKey("User")]
        public int UserId { get; set; }

        [Required, StringLength(100)]
        public string Specialization { get; set; }

        [StringLength(150)]
        public string Certification { get; set; }

        public int? YearsExperience { get; set; }

        [StringLength(500)]
        public string Bio { get; set; }

        public virtual User User { get; set; }
    }

    [Table("TrainerClients")]
    public class TrainerClient
    {
        public TrainerClient()
        {
            AssignedAt = DateTime.UtcNow;
            Status = "Active";
        }

        [Key]
        public int AssignmentId { get; set; }

        [Index("UQ_TrainerClient", 1, IsUnique = true)]
        public int TrainerId { get; set; }

        [Index("UQ_TrainerClient", 2, IsUnique = true)]
        public int ClientId { get; set; }

        public int AssignedBy { get; set; }
        public DateTime AssignedAt { get; set; }

        [Required, StringLength(15)]
        public string Status { get; set; }

        [ForeignKey("TrainerId")]
        public virtual User Trainer { get; set; }

        [ForeignKey("ClientId")]
        public virtual User Client { get; set; }

        [ForeignKey("AssignedBy")]
        public virtual User AssignedByUser { get; set; }
    }
}