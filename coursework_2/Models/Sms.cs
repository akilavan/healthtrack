using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace coursework_2.Models
{
    [Table("SmsMessages")]
    public class SmsMessage
    {
        [Key]
        public int SmsMessageId { get; set; }

        public int? UserId { get; set; }

        [StringLength(30)]
        public string Phone { get; set; }

        [StringLength(500)]
        public string Message { get; set; }

        [StringLength(30)]
        public string Purpose { get; set; }

        [StringLength(20)]
        public string Status { get; set; }

        [StringLength(500)]
        public string ProviderResponse { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public static class SmsService
    {
        public const string DefaultApiUrl = "https://sms.txtmsg.lk/api/v3/sms/send";
        private const int MaxAttempts = 5;

        public static bool Enabled
        {
            get { return SettingsService.Current.SmsEnabled; }
        }

        public static int AttemptLimit
        {
            get { return MaxAttempts; }
        }

        public static string Normalize(string phone)
        {
            return ValidationHelper.NormalizePhone(phone);
        }

        public static string GenerateCode()
        {
            byte[] bytes = new byte[4];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            int value = BitConverter.ToInt32(bytes, 0) & 0x7FFFFFFF;
            return (value % 1000000).ToString("000000");
        }

        public static void IssueVerificationCode(User user)
        {
            user.PhoneVerified = false;
            user.PhoneVerificationCode = GenerateCode();
            user.PhoneVerificationSentAt = DateTime.UtcNow;
            user.PhoneVerificationExpiresAt = DateTime.UtcNow.AddMinutes(10);
            user.PhoneVerificationAttempts = 0;
        }

        public static bool SendVerificationCode(User user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.PhoneVerificationCode)) return false;

            string company = SettingsService.Current.CompanyName;
            return Send(user.Phone,
                company + ": your verification code is " + user.PhoneVerificationCode +
                ". It expires in 10 minutes.",
                "Verification", user.UserId);
        }

        public static bool Send(string phone, string message, string purpose, int? userId = null)
        {
            SystemSetting settings = SettingsService.Current;

            if (!settings.SmsEnabled) return false;

            string to = Normalize(phone);

            var log = new SmsMessage
            {
                UserId = userId,
                Phone = to,
                Message = message,
                Purpose = purpose,
                Status = "Failed",
                CreatedAt = DateTime.UtcNow
            };

            if (string.IsNullOrWhiteSpace(to) ||
                string.IsNullOrWhiteSpace(settings.SmsApiKey) ||
                string.IsNullOrWhiteSpace(settings.SmsSenderId))
            {
                log.ProviderResponse = "SMS enabled but phone, API key or sender ID is missing.";
                SaveLog(log);
                return false;
            }

            try
            {
                string url = string.IsNullOrWhiteSpace(settings.SmsApiUrl)
                    ? DefaultApiUrl
                    : settings.SmsApiUrl.Trim();

                string json = new JavaScriptSerializer().Serialize(new
                {
                    recipient = to,
                    sender_id = settings.SmsSenderId.Trim(),
                    type = "plain",
                    message = message
                });

                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Accept = "application/json";
                request.Headers["Authorization"] = "Bearer " + settings.SmsApiKey.Trim();
                request.Timeout = 15000;

                byte[] body = Encoding.UTF8.GetBytes(json);
                request.ContentLength = body.Length;

                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(body, 0, body.Length);
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    string providerResponse = reader.ReadToEnd();
                    log.ProviderResponse = Truncate(providerResponse, 500);
                    log.Status = IsProviderError(providerResponse) ? "Failed" : "Sent";
                }
            }
            catch (WebException ex)
            {
                log.Status = "Failed";

                try
                {
                    if (ex.Response != null)
                    {
                        using (var reader = new StreamReader(ex.Response.GetResponseStream()))
                        {
                            log.ProviderResponse = Truncate(reader.ReadToEnd(), 500);
                        }
                    }
                    else
                    {
                        log.ProviderResponse = Truncate(ex.Message, 500);
                    }
                }
                catch
                {
                    log.ProviderResponse = Truncate(ex.Message, 500);
                }
            }
            catch (Exception ex)
            {
                log.Status = "Failed";
                log.ProviderResponse = Truncate(ex.Message, 500);
            }

            SaveLog(log);
            return log.Status == "Sent";
        }

        public static void SaveLog(SmsMessage log)
        {
            try
            {
                using (var db = new HealthTrackContext())
                {
                    db.SmsMessages.Add(log);
                    db.SaveChanges();
                }
            }
            catch { }
        }

        private static bool IsProviderError(string response)
        {
            if (string.IsNullOrWhiteSpace(response)) return false;

            string body = response.ToLowerInvariant();
            return body.Contains("\"status\":\"error\"")
                || body.Contains("\"status\":\"failed\"")
                || body.Contains("\"status\":false")
                || body.Contains("\"success\":false");
        }

        private static string Truncate(string value, int max)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= max ? value : value.Substring(0, max);
        }
    }
}
