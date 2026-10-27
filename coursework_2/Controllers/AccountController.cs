using coursework_2.Models;
using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.WebPages;
using ValidationHelper = coursework_2.Models.ValidationHelper;

namespace coursework_2.Controllers
{
    public class AccountController : Controller
    {
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Dashboard");

            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string email, string password, bool rememberMe = false)
        {
            if (!RecaptchaService.Verify(Request.Form["g-recaptcha-response"], Request.UserHostAddress))
            {
                ViewBag.Error = "Please complete the reCAPTCHA verification.";
                return View();
            }

            email = (email ?? "").Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Email and password are required.";
                return View();
            }

            using (var db = new HealthTrackContext())
            {
                var user = db.Users.Include("Role").FirstOrDefault(u => u.Email == email);

                if (user == null)
                {
                    ViewBag.Error = "Invalid email or password.";
                    return View();
                }
                if (!user.IsActive)
                {
                    WriteAudit(db, user.UserId, "LoginInactive", user.Email);
                    ViewBag.Error = "Your account is deactivated. Please contact an administrator.";
                    return View();
                }

                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                {
                    ViewBag.Error = "Account locked until " +
                                    user.LockoutEnd.Value.ToLocalTime().ToString("f") + ".";
                    return View();
                }


                bool passwordOk;
                try { passwordOk = Crypto.VerifyHashedPassword(user.PasswordHash, password); }
                catch { passwordOk = false; }

                if (!passwordOk)
                {
                    user.FailedLogins++;
                    if (user.FailedLogins >= 5)
                    {
                        user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                        user.FailedLogins = 0;
                    }

                    db.SaveChanges();
                    WriteAudit(db, user.UserId, "LoginFailed", user.Email);

                    ViewBag.Error = "Invalid email or password.";
                    return View();
                }

                user.FailedLogins = 0;
                user.LockoutEnd = null;
                user.LastLoginAt = DateTime.UtcNow;
                db.SaveChanges();

                WriteAudit(db, user.UserId, "Login", user.Email);
                SignIn(user, rememberMe);

                if (SmsService.Enabled && !user.PhoneVerified)
                    return RedirectToAction("VerifyPhone");
            }

            var returnUrl = Request.QueryString["ReturnUrl"];
            if (Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Dashboard");

            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(string fullName, string email, string phone,
            DateTime? dateOfBirth, string gender, string role,
            string password, string confirmPassword)
        {
            if (!RecaptchaService.Verify(Request.Form["g-recaptcha-response"], Request.UserHostAddress))
            {
                ViewBag.Error = "Please complete the reCAPTCHA verification.";
                return View();
            }

            fullName = (fullName ?? "").Trim();
            email = (email ?? "").Trim().ToLowerInvariant();
            phone = (phone ?? "").Trim();
            gender = (gender ?? "").Trim();
            role = (role ?? "").Trim();

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Full name and email are required.";
                return View();
            }
            if (!ValidationHelper.IsValidEmail(email))
            {
                ViewBag.Error = "Enter a valid email address (e.g. name@example.com).";
                return View();
            }

            if (!string.IsNullOrWhiteSpace(phone) && !ValidationHelper.IsValidPhone(phone))
            {
                ViewBag.Error = "Enter a valid phone number (e.g. 0712345678 or +94712345678).";
                return View();
            }

            if (SmsService.Enabled && string.IsNullOrWhiteSpace(phone))
            {
                ViewBag.Error = "Phone number is required because SMS verification is enabled.";
                return View();
            }

            if (!ValidationHelper.IsValidPassword(password))
            {
                ViewBag.Error = ValidationHelper.PasswordRule;
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View();
            }

            if (role != "Client" && role != "Trainer")
            {
                ViewBag.Error = "Please select a valid account type.";
                return View();
            }

            if (gender != "" && gender != "Male" && gender != "Female" && gender != "Other")
            {
                ViewBag.Error = "Please select a valid gender.";
                return View();
            }

            using (var db = new HealthTrackContext())
            {
                if (db.Users.Any(u => u.Email == email))
                {
                    ViewBag.Error = "An account with this email already exists.";
                    return View();
                }

                var roleEntity = db.Roles.FirstOrDefault(r => r.RoleName == role);
                if (roleEntity == null)
                {
                    ViewBag.Error = "Role configuration error. Contact the administrator.";
                    return View();
                }

                var user = new coursework_2.Models.User
                {
                    RoleId = roleEntity.RoleId,
                    Role = roleEntity,
                    Email = email,
                    PasswordHash = Crypto.HashPassword(password),
                    FullName = fullName,
                    Phone = ValidationHelper.NormalizePhone(phone),
                    PhoneVerified = !SmsService.Enabled,
                    DateOfBirth = dateOfBirth,
                    Gender = string.IsNullOrWhiteSpace(gender) ? null : gender,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                db.Users.Add(user);

                try
                {
                    db.SaveChanges();
                }
                catch (DbUpdateException)
                {
                    ViewBag.Error = "Registration failed. Please try again.";
                    return View();
                }

                WriteAudit(db, user.UserId, "Register", role);

                if (SmsService.Enabled)
                {
                    SmsService.IssueVerificationCode(user);
                    db.SaveChanges();
                    SmsService.SendVerificationCode(user);
                }

                SignIn(user, false);
            }

            if (SmsService.Enabled)
                return RedirectToAction("VerifyPhone");

            return RedirectToAction("Index", "Dashboard");
        }

        [Authorize]
        [HttpGet]
        public ActionResult VerifyPhone()
        {
            using (var db = new HealthTrackContext())
            {
                var user = db.Users.Include("Role").FirstOrDefault(u => u.Email == User.Identity.Name);
                if (user == null) return RedirectToAction("Login");
                if (!SmsService.Enabled || user.PhoneVerified) return RedirectToAction("Index", "Dashboard");

                FillVerificationView(user);
                return View();
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult VerifyPhone(string code)
        {
            using (var db = new HealthTrackContext())
            {
                var user = db.Users.Include("Role").FirstOrDefault(u => u.Email == User.Identity.Name);
                if (user == null) return RedirectToAction("Login");
                if (!SmsService.Enabled || user.PhoneVerified) return RedirectToAction("Index", "Dashboard");

                FillVerificationView(user);

                if (string.IsNullOrWhiteSpace(code))
                {
                    ViewBag.Error = "Enter the 6-digit code.";
                    return View();
                }

                if (!user.PhoneVerificationExpiresAt.HasValue ||
                    user.PhoneVerificationExpiresAt.Value < DateTime.UtcNow)
                {
                    ViewBag.Error = "That code has expired. Request a new one.";
                    return View();
                }

                if (user.PhoneVerificationAttempts >= SmsService.AttemptLimit)
                {
                    ViewBag.Error = "Too many incorrect attempts. Request a new code.";
                    return View();
                }

                if (!string.Equals((user.PhoneVerificationCode ?? "").Trim(), code.Trim()))
                {
                    user.PhoneVerificationAttempts++;
                    db.SaveChanges();
                    WriteAudit(db, user.UserId, "PhoneVerificationFailed", null);
                    ViewBag.Error = "Incorrect code. Please try again.";
                    return View();
                }

                user.PhoneVerified = true;
                user.PhoneVerificationCode = null;
                user.PhoneVerificationExpiresAt = null;
                user.PhoneVerificationSentAt = null;
                user.PhoneVerificationAttempts = 0;
                db.SaveChanges();

                WriteAudit(db, user.UserId, "PhoneVerified", null);
                TempData["Success"] = "Phone number verified.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResendPhoneCode()
        {
            using (var db = new HealthTrackContext())
            {
                var user = db.Users.Include("Role").FirstOrDefault(u => u.Email == User.Identity.Name);
                if (user == null) return RedirectToAction("Login");
                if (!SmsService.Enabled || user.PhoneVerified) return RedirectToAction("Index", "Dashboard");

                if (string.IsNullOrWhiteSpace(user.Phone))
                {
                    TempData["Error"] = "Add a phone number in your profile first.";
                    return RedirectToAction("VerifyPhone");
                }

                if (user.PhoneVerificationSentAt.HasValue &&
                    user.PhoneVerificationSentAt.Value > DateTime.UtcNow.AddSeconds(-60))
                {
                    TempData["Error"] = "Please wait a minute before requesting another code.";
                    return RedirectToAction("VerifyPhone");
                }

                SmsService.IssueVerificationCode(user);
                db.SaveChanges();

                bool sent = SmsService.SendVerificationCode(user);
                TempData[sent ? "Success" : "Error"] = sent
                    ? "A new code was sent to " + user.Phone + "."
                    : "Could not send the SMS. Check the SMS settings and log.";

                return RedirectToAction("VerifyPhone");
            }
        }

        private void FillVerificationView(coursework_2.Models.User user)
        {
            ViewBag.Phone = user.Phone;
            ViewBag.CompanyName = SettingsService.Current.CompanyName;
            ViewBag.HasPhone = !string.IsNullOrWhiteSpace(user.Phone);
            if (user.PhoneVerificationSentAt.HasValue)
                ViewBag.CodeSentAt = SettingsService.FromUtc(user.PhoneVerificationSentAt.Value);
        }

        public ActionResult Logout()
        {
            int? userId = Session["UserId"] as int?;

            using (var db = new HealthTrackContext())
            {
                WriteAudit(db, userId, "Logout", null);
            }

            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Login");
        }

        private void SignIn(coursework_2.Models.User user, bool rememberMe)
        {
            Session["UserId"] = user.UserId;
            Session["Role"] = user.Role.RoleName;
            Session["FullName"] = user.FullName;

            var ticket = new FormsAuthenticationTicket(
                1,
                user.Email,
                DateTime.Now,
                DateTime.Now.AddMinutes(rememberMe ? 60 * 24 * 7 : 60),
                rememberMe,
                user.Role.RoleName);

            var cookie = new HttpCookie(
                FormsAuthentication.FormsCookieName,
                FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true,
                Secure = Request.IsSecureConnection
            };

            if (rememberMe)
                cookie.Expires = ticket.Expiration;

            Response.Cookies.Add(cookie);
        }

        private void WriteAudit(HealthTrackContext db, int? userId, string action, string entityName)
        {
            db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                IpAddress = Request.UserHostAddress,
                CreatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
        }
    }
}