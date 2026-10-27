using coursework_2.Models;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;
using ValidationHelper = coursework_2.Models.ValidationHelper;

namespace coursework_2.Controllers
{
    public class ProfileController : BaseController
    {
        public ActionResult Index()
        {
            ViewBag.Title = "My Profile";
            ViewBag.Subtitle = "Account";

            var user = db.Users.Include("Role").Include("ClientProfile").Include("TrainerProfile")
                .FirstOrDefault(u => u.UserId == CurrentUserId);

            if (user == null) return HttpNotFound();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(string fullName, string phone, string gender, DateTime? dateOfBirth,
            decimal? heightCm, decimal? goalWeightKg, string fitnessGoal, string activityLevel, string medicalNotes,
            string specialization, string certification, int? yearsExperience, string bio,
            HttpPostedFileBase photo = null, bool removePhoto = false)
        {
            ViewBag.Title = "My Profile";
            ViewBag.Subtitle = "Account";

            var user = db.Users.Include("Role").Include("ClientProfile").Include("TrainerProfile")
                .FirstOrDefault(u => u.UserId == CurrentUserId);

            if (user == null) return HttpNotFound();

            if (string.IsNullOrWhiteSpace(fullName))
            {
                ViewBag.Error = "Full name is required.";
                return View(user);
            }
            if (!string.IsNullOrWhiteSpace(phone) && !ValidationHelper.IsValidPhone(phone))
            {
                ViewBag.Error = "Enter a valid phone number (e.g. 0712345678 or +94712345678).";
                return View(user);
            }

            string newPhone = ValidationHelper.NormalizePhone(phone);

            if (SmsService.Enabled && string.IsNullOrWhiteSpace(newPhone))
            {
                ViewBag.Error = "Phone number is required because SMS verification is enabled.";
                return View(user);
            }

            bool phoneChanged = !string.Equals(user.Phone, newPhone, StringComparison.Ordinal);

            if (!string.IsNullOrEmpty(gender) && gender != "Male" && gender != "Female" && gender != "Other")
            {
                ViewBag.Error = "Invalid gender.";
                return View(user);
            }

            if (heightCm.HasValue && (heightCm < 50 || heightCm > 250))
            {
                ViewBag.Error = "Height must be between 50 and 250 cm.";
                return View(user);
            }
            if (photo != null && photo.ContentLength > 0)
            {
                string ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
                string[] allowed = { ".png", ".jpg", ".jpeg", ".gif", ".webp" };

                if (!allowed.Contains(ext))
                {
                    ViewBag.Error = "Photo must be a PNG, JPG, GIF or WEBP file.";
                    return View(user);
                }

                if (photo.ContentLength > 3 * 1024 * 1024)
                {
                    ViewBag.Error = "Photo must be 3 MB or smaller.";
                    return View(user);
                }

                string folder = Server.MapPath("~/assets/uploads/profiles");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                string fileName = "user-" + user.UserId + "-" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ext;
                photo.SaveAs(Path.Combine(folder, fileName));

                DeletePhotoFile(user.PhotoPath);
                user.PhotoPath = "~/assets/uploads/profiles/" + fileName;
            }
            else if (removePhoto)
            {
                DeletePhotoFile(user.PhotoPath);
                user.PhotoPath = null;
            }
            user.FullName = fullName.Trim();
            user.Phone = newPhone;
            user.Gender = string.IsNullOrWhiteSpace(gender) ? null : gender;
            user.DateOfBirth = dateOfBirth;

            if (user.Role.RoleName == "Client")
            {
                if (user.ClientProfile == null)
                {
                    user.ClientProfile = new ClientProfile { UserId = user.UserId };
                    db.ClientProfiles.Add(user.ClientProfile);
                }

                if (heightCm.HasValue) user.ClientProfile.HeightCm = heightCm.Value;
                user.ClientProfile.GoalWeightKg = goalWeightKg;
                user.ClientProfile.FitnessGoal = (fitnessGoal ?? "").Trim();
                user.ClientProfile.ActivityLevel = (activityLevel ?? "").Trim();
                user.ClientProfile.MedicalNotes = (medicalNotes ?? "").Trim();
            }
            else if (user.Role.RoleName == "Trainer")
            {
                if (user.TrainerProfile == null)
                {
                    user.TrainerProfile = new TrainerProfile { UserId = user.UserId, Specialization = "General Fitness" };
                    db.TrainerProfiles.Add(user.TrainerProfile);
                }

                user.TrainerProfile.Specialization = string.IsNullOrWhiteSpace(specialization)
                    ? "General Fitness" : specialization.Trim();
                user.TrainerProfile.Certification = (certification ?? "").Trim();
                user.TrainerProfile.YearsExperience = yearsExperience;
                user.TrainerProfile.Bio = (bio ?? "").Trim();
            }

                    
            Audit("ProfileUpdated", "Users", user.UserId);
            db.SaveChanges();

            Session["FullName"] = user.FullName;

            if (SmsService.Enabled && phoneChanged)
            {
                SmsService.IssueVerificationCode(user);
                db.SaveChanges();
                SmsService.SendVerificationCode(user);

                Audit("PhoneVerificationSent", "Users", user.UserId);
                db.SaveChanges();

                TempData["Success"] = "Profile saved. Enter the code sent to your new phone number.";
                return RedirectToAction("VerifyPhone", "Account");
            }

            TempData["Success"] = "Profile saved.";
            return RedirectToAction("Index");
        }
        private void DeletePhotoFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("~/assets/uploads/profiles/")) return;

            try
            {
                string physical = Server.MapPath(path);
                if (System.IO.File.Exists(physical)) System.IO.File.Delete(physical);
            }
            catch { }
        }
    }
}