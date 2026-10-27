using coursework_2.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;
using ValidationHelper = coursework_2.Models.ValidationHelper;

namespace coursework_2.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SettingsController : BaseController
    {
        private static readonly string[] AllowedExtensions = { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg" };
        private const int MaxLogoBytes = 2 * 1024 * 1024;

        [HttpGet]
        public ActionResult Index()
        {
            ViewBag.Title = "System Settings";
            ViewBag.Subtitle = "Administration";
            ViewBag.RecentSms = db.SmsMessages
                .OrderByDescending(s => s.SmsMessageId)
                .Take(10)
                .ToList();
            return View(GetOrCreate());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(string companyName, string tagline, string supportEmail,
            string supportPhone, int cancellationWindowHours, string timeZoneId,
            HttpPostedFileBase logo, bool removeLogo = false)
        {
            ViewBag.Title = "System Settings";
            ViewBag.Subtitle = "Administration";

            var settings = GetOrCreate();

            companyName = (companyName ?? "").Trim();
            tagline = (tagline ?? "").Trim();
            supportEmail = (supportEmail ?? "").Trim().ToLowerInvariant();
            supportPhone = (supportPhone ?? "").Trim();

            if (string.IsNullOrWhiteSpace(companyName))
            {
                ViewBag.Error = "Company name is required.";
                return View(settings);
            }
            if (!string.IsNullOrWhiteSpace(supportEmail) && !ValidationHelper.IsValidEmail(supportEmail))
            {
                ViewBag.Error = "Support email is not valid (e.g. support@example.com).";
                return View(settings);
            }

            if (!string.IsNullOrWhiteSpace(supportPhone) && !ValidationHelper.IsValidPhone(supportPhone))
            {
                ViewBag.Error = "Support phone is not valid (e.g. 0712345678 or +94712345678).";
                return View(settings);
            }
            if (string.IsNullOrWhiteSpace(timeZoneId) ||
                !TimeZoneInfo.GetSystemTimeZones().Any(z => z.Id == timeZoneId))
            {
                ViewBag.Error = "Select a valid time zone.";
                return View(settings);
            }

            if (cancellationWindowHours < 0 || cancellationWindowHours > 720)
            {
                ViewBag.Error = "Cancellation window must be between 0 and 720 hours (30 days).";
                return View(settings);
            }

            if (logo != null && logo.ContentLength > 0)
            {
                string ext = Path.GetExtension(logo.FileName).ToLowerInvariant();

                if (!AllowedExtensions.Contains(ext))
                {
                    ViewBag.Error = "Logo must be a PNG, JPG, GIF, WEBP or SVG file.";
                    return View(settings);
                }

                if (logo.ContentLength > MaxLogoBytes)
                {
                    ViewBag.Error = "Logo must be 2 MB or smaller.";
                    return View(settings);
                }

                string folder = Server.MapPath("~/assets/uploads");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                string fileName = "logo-" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ext;
                logo.SaveAs(Path.Combine(folder, fileName));

                DeleteLogoFile(settings.LogoPath);
                settings.LogoPath = "~/assets/uploads/" + fileName;
            }
            else if (removeLogo)
            {
                DeleteLogoFile(settings.LogoPath);
                settings.LogoPath = null;
            }

            settings.CompanyName = companyName;
            settings.Tagline = string.IsNullOrWhiteSpace(tagline) ? null : tagline;
            settings.SupportEmail = string.IsNullOrWhiteSpace(supportEmail) ? null : supportEmail;
            settings.SupportPhone = ValidationHelper.NormalizePhone(supportPhone);
            settings.TimeZoneId = timeZoneId;
            settings.CancellationWindowHours = cancellationWindowHours;
            settings.UpdatedAt = DateTime.UtcNow;

            db.SaveChanges();
            SettingsService.Clear();
            Audit("SettingsUpdated", "SystemSettings", settings.SettingId);
            db.SaveChanges();

            TempData["Success"] = "System settings saved.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Sms(bool smsEnabled = false, string smsApiKey = null, string smsSenderId = null, string smsApiUrl = null)
        {
            ViewBag.Title = "System Settings";
            ViewBag.Subtitle = "Administration";

            var settings = GetOrCreate();

            smsApiKey = (smsApiKey ?? "").Trim();
            smsSenderId = (smsSenderId ?? "").Trim();
            smsApiUrl = (smsApiUrl ?? "").Trim();

            if (smsEnabled)
            {
                if (string.IsNullOrWhiteSpace(smsApiKey))
                {
                    TempData["Error"] = "API key is required when SMS is enabled.";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrWhiteSpace(smsSenderId) || smsSenderId.Length > 11)
                {
                    TempData["Error"] = "Sender ID is required (maximum 11 characters).";
                    return RedirectToAction("Index");
                }

                Uri uri;
                if (!Uri.TryCreate(smsApiUrl, UriKind.Absolute, out uri) ||
                    (uri.Scheme != "http" && uri.Scheme != "https"))
                {
                    TempData["Error"] = "Enter a valid SMS API URL (http or https).";
                    return RedirectToAction("Index");
                }
            }

            settings.SmsEnabled = smsEnabled;
            settings.SmsApiKey = string.IsNullOrWhiteSpace(smsApiKey) ? null : smsApiKey;
            settings.SmsSenderId = string.IsNullOrWhiteSpace(smsSenderId) ? null : smsSenderId;
            settings.SmsApiUrl = string.IsNullOrWhiteSpace(smsApiUrl) ? SmsService.DefaultApiUrl : smsApiUrl;
            settings.UpdatedAt = DateTime.UtcNow;

            db.SaveChanges();
            SettingsService.Clear();
            Audit("SettingsUpdated", "SystemSettings", settings.SettingId);
            db.SaveChanges();

            TempData["Success"] = smsEnabled
                ? "SMS gateway enabled. Verification codes and appointment SMS will be sent."
                : "SMS gateway disabled. Verification and SMS notifications are skipped.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TestSms(string testPhone)
        {
            var settings = GetOrCreate();

            if (!settings.SmsEnabled)
            {
                TempData["Error"] = "Enable SMS before sending a test message.";
                return RedirectToAction("Index");
            }

            if (!ValidationHelper.IsValidPhone(testPhone))
            {
                TempData["Error"] = "Enter a valid test phone number (e.g. 0712345678).";
                return RedirectToAction("Index");
            }

            bool sent = SmsService.Send(testPhone,
                SettingsService.Current.CompanyName + ": test message from your HealthTrack dashboard.",
                "Test", CurrentUserId);

            if (sent)
                TempData["Success"] = "Test SMS sent to " + ValidationHelper.NormalizePhone(testPhone) + ".";
            else
                TempData["Error"] = "Test SMS failed. Check the SMS log below for the provider response.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Captcha(bool recaptchaEnabled = false, string recaptchaSiteKey = null, string recaptchaSecretKey = null)
        {
            var settings = GetOrCreate();

            recaptchaSiteKey = (recaptchaSiteKey ?? "").Trim();
            recaptchaSecretKey = (recaptchaSecretKey ?? "").Trim();

            if (recaptchaEnabled)
            {
                if (string.IsNullOrWhiteSpace(recaptchaSiteKey) || string.IsNullOrWhiteSpace(recaptchaSecretKey))
                {
                    TempData["Error"] = "Site key and secret key are required when reCAPTCHA is enabled.";
                    return RedirectToAction("Index");
                }
            }

            settings.RecaptchaEnabled = recaptchaEnabled;
            settings.RecaptchaSiteKey = string.IsNullOrWhiteSpace(recaptchaSiteKey) ? null : recaptchaSiteKey;
            settings.RecaptchaSecretKey = string.IsNullOrWhiteSpace(recaptchaSecretKey) ? null : recaptchaSecretKey;
            settings.UpdatedAt = DateTime.UtcNow;

            db.SaveChanges();
            SettingsService.Clear();
            Audit("SettingsUpdated", "SystemSettings", settings.SettingId);
            db.SaveChanges();

            TempData["Success"] = recaptchaEnabled
                ? "reCAPTCHA enabled for login and registration."
                : "reCAPTCHA disabled. Login and registration work without a captcha.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LandingPage(bool landingPageEnabled = false)
        {
            var settings = GetOrCreate();

            settings.LandingPageEnabled = landingPageEnabled;
            settings.UpdatedAt = DateTime.UtcNow;

            db.SaveChanges();
            SettingsService.Clear();
            Audit("SettingsUpdated", "SystemSettings", settings.SettingId);
            db.SaveChanges();

            TempData["Success"] = landingPageEnabled
                ? "Public landing page enabled."
                : "Public landing page disabled. Signed-out visitors will be sent to the login page.";

            return RedirectToAction("Index");
        }

        private SystemSetting GetOrCreate()
        {
            var settings = db.SystemSettings.FirstOrDefault();

            if (settings == null)
            {
                settings = new SystemSetting
                {
                    CompanyName = "HealthTrack",
                    Tagline = "Personal Health & Fitness Management",
                    CancellationWindowHours = 24,
                    TimeZoneId = TimeZoneInfo.Local.Id,
                    SmsApiUrl = SmsService.DefaultApiUrl,
                    UpdatedAt = DateTime.UtcNow
                };
                db.SystemSettings.Add(settings);
                db.SaveChanges();
            }

            return settings;
        }

        private void DeleteLogoFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("~/assets/uploads/")) return;

            try
            {
                string physical = Server.MapPath(path);
                if (System.IO.File.Exists(physical)) System.IO.File.Delete(physical);
            }
            catch { }
        }
    }
}