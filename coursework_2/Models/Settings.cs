using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace coursework_2.Models
{
    [Table("SystemSettings")]
    public class SystemSetting
    {
        [Key]
        public int SettingId { get; set; }

        [Required, StringLength(120)]
        public string CompanyName { get; set; }

        [StringLength(200)]
        public string Tagline { get; set; }

        [StringLength(300)]
        public string LogoPath { get; set; }

        [StringLength(150)]
        public string SupportEmail { get; set; }

        [StringLength(30)]
        public string SupportPhone { get; set; }
        [StringLength(100)]
        public string TimeZoneId { get; set; }

        public int CancellationWindowHours { get; set; }

        public bool SmsEnabled { get; set; }

        [StringLength(200)]
        public string SmsApiKey { get; set; }

        [StringLength(20)]
        public string SmsSenderId { get; set; }

        [StringLength(300)]
        public string SmsApiUrl { get; set; }

        public bool RecaptchaEnabled { get; set; }

        [StringLength(100)]
        public string RecaptchaSiteKey { get; set; }

        [StringLength(100)]
        public string RecaptchaSecretKey { get; set; }

        public bool LandingPageEnabled { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}