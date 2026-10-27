using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using coursework_2.Models;

namespace coursework_2.Controllers
{
    public abstract class BaseController : Controller
    {
        protected readonly HealthTrackContext db = new HealthTrackContext();

        protected User CurrentUser { get; private set; }
        protected int CurrentUserId { get { return CurrentUser == null ? 0 : CurrentUser.UserId; } }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            int? id = Session["UserId"] as int?;

            if (id.HasValue)
                CurrentUser = db.Users.Include("Role").FirstOrDefault(u => u.UserId == id.Value);

            if (CurrentUser == null && User.Identity.IsAuthenticated)
            {
                string email = User.Identity.Name;
                CurrentUser = db.Users.Include("Role").FirstOrDefault(u => u.Email == email);
                if (CurrentUser != null)
                    Session["UserId"] = CurrentUser.UserId;
            }

            if (CurrentUser != null && SmsService.Enabled && !CurrentUser.PhoneVerified)
            {
                string controller = Convert.ToString(filterContext.RouteData.Values["controller"]);
                string action = Convert.ToString(filterContext.RouteData.Values["action"]);

                bool allowed = controller == "Profile"
                    || (controller == "Account" && (action == "VerifyPhone" || action == "Logout"));

                if (!allowed)
                {
                    filterContext.Result = new RedirectToRouteResult(
                        new System.Web.Routing.RouteValueDictionary(
                            new { controller = "Account", action = "VerifyPhone" }));
                    return;
                }
            }

            try { GenerateDailyReminders(); } catch { }
            ViewBag.CurrentUser = CurrentUser;
            ViewBag.UnreadNotifications = CurrentUser == null
                ? 0
                : db.Notifications.Count(n => n.UserId == CurrentUser.UserId && !n.IsRead);
        }

        protected void Audit(string action, string entityName = null, int? entityId = null)
        {
            db.AuditLogs.Add(new AuditLog
            {
                UserId = CurrentUserId == 0 ? (int?)null : CurrentUserId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                IpAddress = Request.UserHostAddress,
                CreatedAt = DateTime.UtcNow
            });
        }
        protected void Notify(int userId, string type, string title, string message, int? relatedId = null, bool sms = false)
        {
            db.Notifications.Add(new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                ScheduledFor = DateTime.UtcNow,
                SentAt = DateTime.UtcNow,
                IsRead = false,
                RelatedId = relatedId
            });

            if (sms && SmsService.Enabled)
            {
                string phone = db.Users
                    .Where(u => u.UserId == userId)
                    .Select(u => u.Phone)
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(phone))
                {
                    SmsService.Send(phone,
                        SettingsService.Current.CompanyName + ": " + title + " - " + message,
                        type, userId);
                }
            }
        }
        private void GenerateDailyReminders()
        {
            if (CurrentUser == null || CurrentUser.Role == null) return;
            if (CurrentUser.Role.RoleName != "Client") return;

            DateTime today = DateTime.Today;
            string key = "RemindersGenerated_" + today.ToString("yyyyMMdd");
            if (Session[key] != null) return;
            Session[key] = true;

            DateTime tomorrow = today.AddDays(1);
            DateTime now = DateTime.Now;
            DateTime in24 = now.AddHours(24);
            int uid = CurrentUser.UserId;
            bool changed = false;

            var planIds = db.WorkoutPlans
                .Where(p => p.ClientId == uid && p.Status == "Active")
                .Select(p => p.PlanId)
                .ToList();

            if (planIds.Count > 0)
            {
                int todayDow = (int)today.DayOfWeek;

                var planned = db.WorkoutPlanItems
                    .Where(i => planIds.Contains(i.PlanId) && i.DayOfWeek == todayDow)
                    .Select(i => i.PlanItemId)
                    .ToList();

                if (planned.Count > 0)
                {
                    var logged = db.WorkoutLogs
                        .Where(w => w.ClientId == uid && w.LogDate == today && w.PlanItemId.HasValue)
                        .Select(w => w.PlanItemId.Value)
                        .ToList();

                    int pending = planned.Count(i => !logged.Contains(i));

                    bool exists = db.Notifications.Any(n =>
                        n.UserId == uid && n.Type == "WorkoutReminder" &&
                        n.ScheduledFor >= today && n.ScheduledFor < tomorrow);

                    if (pending > 0 && !exists)
                    {
                        Notify(uid, "WorkoutReminder", "Workout reminder",
                            pending + " exercise(s) from your plan are not logged yet today.");
                        changed = true;
                    }
                }
            }

            if (db.DietPlans.Any(d => d.ClientId == uid && d.Status == "Active"))
            {
                bool loggedMeal = db.MealLogs.Any(m => m.ClientId == uid && m.LogDate == today);

                bool exists = db.Notifications.Any(n =>
                    n.UserId == uid && n.Type == "MealReminder" &&
                    n.ScheduledFor >= today && n.ScheduledFor < tomorrow);

                if (!loggedMeal && !exists)
                {
                    Notify(uid, "MealReminder", "Meal reminder",
                        "Don't forget to log your meals today.");
                    changed = true;
                }
            }

            var upcoming = db.Appointments
                .Where(a => a.ClientId == uid
                            && a.Status != "Cancelled"
                            && a.StartTime >= now
                            && a.StartTime <= in24)
                .Select(a => new { a.AppointmentId, a.StartTime, a.TrainerId })
                .ToList();

            foreach (var a in upcoming)
            {
                bool exists = db.Notifications.Any(n =>
                    n.UserId == uid && n.Type == "AppointmentReminder" && n.RelatedId == a.AppointmentId);

                if (exists) continue;

                string with = a.TrainerId.HasValue ? "your trainer" : "your healthcare professional";

                Notify(uid, "AppointmentReminder", "Appointment reminder",
                    "You have an appointment with " + with + " on " +
                    a.StartTime.ToString("dd MMM yyyy HH:mm") + ".",
                    a.AppointmentId, true);

                changed = true;
            }

            if (changed) db.SaveChanges();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}