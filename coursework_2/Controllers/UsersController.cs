using coursework_2.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.WebPages;
using ValidationHelper = coursework_2.Models.ValidationHelper;

namespace coursework_2.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : BaseController
    {
        public ActionResult Index(string q, int? roleId)
        {
            ViewBag.Title = "Users";
            ViewBag.Subtitle = "Administration";
            ViewBag.Query = q;
            ViewBag.RoleId = roleId;
            ViewBag.Roles = new SelectList(db.Roles.OrderBy(r => r.RoleName).ToList(), "RoleId", "RoleName", roleId);

            var users = db.Users.Include("Role").AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                users = users.Where(u => u.FullName.Contains(q) || u.Email.Contains(q));
            }

            if (roleId.HasValue)
                users = users.Where(u => u.RoleId == roleId.Value);

            return View(users.OrderBy(u => u.FullName).Take(300).ToList());
        }

        [HttpGet]
        public ActionResult Form(int? id)
        {
            ViewBag.Title = id.HasValue ? "Edit User" : "New User";
            ViewBag.Subtitle = "Users";
            LoadRoles();

            if (!id.HasValue)
                return View(new User { IsActive = true });

            var user = db.Users.Find(id.Value);
            if (user == null) return HttpNotFound();

            LoadRoles(user.RoleId);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Form(int? id, string fullName, string email, int roleId, string phone,
            string gender, DateTime? dateOfBirth, bool isActive, string password)
        {
            ViewBag.Title = id.HasValue ? "Edit User" : "New User";
            ViewBag.Subtitle = "Users";
            LoadRoles(roleId);

            fullName = (fullName ?? "").Trim();
            email = (email ?? "").Trim().ToLowerInvariant();
            phone = (phone ?? "").Trim();
            gender = (gender ?? "").Trim();

            var model = new User
            {
                UserId = id ?? 0,
                FullName = fullName,
                Email = email,
                RoleId = roleId,
                Phone = ValidationHelper.NormalizePhone(phone),
                Gender = string.IsNullOrWhiteSpace(gender) ? null : gender,
                DateOfBirth = dateOfBirth,
                IsActive = isActive
            };

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Full name and email are required.";
                return View(model);
            }
            if (!ValidationHelper.IsValidEmail(email))
            {
                ViewBag.Error = "Invalid email address.";
                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(phone) && !ValidationHelper.IsValidPhone(phone))
            {
                ViewBag.Error = "Enter a valid phone number (e.g. 0712345678 or +94712345678).";
                return View(model);
            }

            if (!db.Roles.Any(r => r.RoleId == roleId))
            {
                ViewBag.Error = "Select a valid role.";
                return View(model);
            }

            if (db.Users.Any(u => u.Email == email && u.UserId != model.UserId))
            {
                ViewBag.Error = "That email is already registered.";
                return View(model);
            }

            if (!string.IsNullOrEmpty(password) && !ValidationHelper.IsValidPassword(password))
            {
                ViewBag.Error = ValidationHelper.PasswordRule;
                return View(model);
            }

            bool isNew = !id.HasValue;

            if (isNew && string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Password is required for a new user.";
                return View(model);
            }

            User user;

            if (isNew)
            {
                user = new User
                {
                    CreatedAt = DateTime.UtcNow,
                    PasswordHash = Crypto.HashPassword(password)
                };
                db.Users.Add(user);
            }
            else
            {
                user = db.Users.Find(id.Value);
                if (user == null) return HttpNotFound();

                if (!string.IsNullOrEmpty(password))
                    user.PasswordHash = Crypto.HashPassword(password);
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.RoleId = model.RoleId;

            bool phoneChanged = !string.Equals(user.Phone, model.Phone, StringComparison.Ordinal);

            user.Phone = model.Phone;
            user.Gender = model.Gender;
            user.DateOfBirth = model.DateOfBirth;
            user.IsActive = model.IsActive;

            if (isNew)
                user.PhoneVerified = !SmsService.Enabled;

            db.SaveChanges();
            Audit(isNew ? "UserCreated" : "UserUpdated", "Users", user.UserId);
            db.SaveChanges();

            if (SmsService.Enabled && phoneChanged)
            {
                if (!string.IsNullOrWhiteSpace(user.Phone))
                {
                    SmsService.IssueVerificationCode(user);
                    db.SaveChanges();
                    SmsService.SendVerificationCode(user);

                    Audit("PhoneVerificationSent", "Users", user.UserId);
                    db.SaveChanges();
                }
                else
                {
                    user.PhoneVerified = false;
                    db.SaveChanges();
                }
            }

            TempData["Success"] = isNew ? "User created." : "User updated.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleActive(int id)
        {
            var user = db.Users.Find(id);
            if (user == null) return HttpNotFound();

            if (user.UserId == CurrentUserId)
            {
                TempData["Error"] = "You cannot deactivate your own account.";
                return RedirectToAction("Index");
            }

            user.IsActive = !user.IsActive;
            Audit(user.IsActive ? "UserActivated" : "UserDeactivated", "Users", id);
            db.SaveChanges();

            TempData["Success"] = user.IsActive ? "User activated." : "User deactivated.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var user = db.Users.Find(id);
            if (user == null) return HttpNotFound();

            if (id == CurrentUserId)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction("Index");
            }

            bool hasData =
                db.WorkoutPlans.Any(p => p.ClientId == id || p.TrainerId == id) ||
                db.DietPlans.Any(p => p.ClientId == id || p.TrainerId == id) ||
                db.Appointments.Any(a => a.ClientId == id || a.TrainerId == id) ||
                db.WorkoutLogs.Any(w => w.ClientId == id) ||
                db.MealLogs.Any(m => m.ClientId == id) ||
                db.HealthMetrics.Any(h => h.ClientId == id) ||
                db.TrainerFeedback.Any(f => f.ClientId == id || f.TrainerId == id) ||
                db.TrainerClients.Any(t => t.TrainerId == id || t.ClientId == id || t.AssignedBy == id) ||
                db.Notifications.Any(n => n.UserId == id);

            if (hasData)
            {
                TempData["Error"] = "This user has records. Deactivate the account instead of deleting it.";
                return RedirectToAction("Index");
            }

            var clientProfile = db.ClientProfiles.FirstOrDefault(p => p.UserId == id);
            if (clientProfile != null) db.ClientProfiles.Remove(clientProfile);

            var trainerProfile = db.TrainerProfiles.FirstOrDefault(p => p.UserId == id);
            if (trainerProfile != null) db.TrainerProfiles.Remove(trainerProfile);

            foreach (var log in db.AuditLogs.Where(l => l.UserId == id).ToList())
                log.UserId = null;

            db.Users.Remove(user);
            Audit("UserDeleted", "Users", id);
            db.SaveChanges();

            TempData["Success"] = "User deleted.";
            return RedirectToAction("Index");
        }

        private void LoadRoles(int? selected = null)
        {
            ViewBag.Roles = new SelectList(
                db.Roles.OrderBy(r => r.RoleName).ToList(), "RoleId", "RoleName", selected);
        }
    }
}