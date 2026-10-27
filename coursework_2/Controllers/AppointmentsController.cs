using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using coursework_2.Models;

namespace coursework_2.Controllers
{
    public class AppointmentsController : BaseController
    {
        private static readonly string[] Statuses = { "Pending", "Confirmed", "Completed", "Cancelled" };

        public ActionResult Index()
        {
            ViewBag.Title = "Appointments";
            ViewBag.Subtitle = "Sessions";

            if (User.IsInRole("Client"))
            {
                var mine = db.Appointments.Include("Trainer").Include("Provider")
                    .Where(a => a.ClientId == CurrentUserId)
                    .OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.StartTime).ToList();
                return View(mine);
            }

            if (User.IsInRole("Trainer"))
            {
                var mine = db.Appointments.Include("Client")
                    .Where(a => a.TrainerId == CurrentUserId)
                    .OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.StartTime).ToList();
                return View(mine);
            }

            var all = db.Appointments.Include("Client").Include("Trainer").Include("Provider")
                .OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.StartTime).Take(300).ToList();
            return View(all);
        }

        [Authorize(Roles = "Client")]
        [HttpGet]
        public ActionResult Book()
        {
            ViewBag.Title = "Book Appointment";
            ViewBag.Subtitle = "Appointments";
            LoadTrainers();
            LoadProviders();

            if (!HasActiveTrainer())
                ViewBag.Error = "You need an assigned trainer before requesting an appointment. Please contact the administrator.";

            return View();
        }

        [Authorize(Roles = "Client")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Book(int? trainerId, int? providerId, DateTime date, string time, string purpose)
        {
            ViewBag.Title = "Book Appointment";
            ViewBag.Subtitle = "Appointments";
            LoadTrainers(trainerId);
            LoadProviders(providerId);

            if (!HasActiveTrainer())
            {
                ViewBag.Error = "You need an assigned trainer before requesting an appointment. Please contact the administrator.";
                return View();
            }

            if (trainerId.HasValue == providerId.HasValue)
            {
                ViewBag.Error = "Choose either a trainer or a healthcare professional.";
                return View();
            }

            if (trainerId.HasValue &&
                !db.Users.Any(u => u.UserId == trainerId.Value && u.Role.RoleName == "Trainer" && u.IsActive))
            {
                ViewBag.Error = "Select a valid trainer.";
                return View();
            }

            if (trainerId.HasValue &&
                !db.TrainerClients.Any(t => t.ClientId == CurrentUserId && t.TrainerId == trainerId.Value && t.Status == "Active"))
            {
                ViewBag.Error = "You can only book appointments with your assigned trainer.";
                return View();
            }

            if (providerId.HasValue &&
                !db.HealthcareProviders.Any(p => p.ProviderId == providerId.Value && p.IsActive))
            {
                ViewBag.Error = "Select a valid healthcare professional.";
                return View();
            }

            TimeSpan slot;
            if (!TimeSpan.TryParse(time, out slot))
            {
                ViewBag.Error = "Select a valid time.";
                return View();
            }

            DateTime start = SettingsService.ToServerTime(date.Date + slot);

            if (start <= DateTime.Now)
            {
                ViewBag.Error = "Pick a future date and time.";
                return View();
            }

            bool clash = trainerId.HasValue
                ? db.Appointments.Any(a => a.TrainerId == trainerId.Value && a.Status != "Cancelled" && a.StartTime == start)
                : db.Appointments.Any(a => a.ProviderId == providerId.Value && a.Status != "Cancelled" && a.StartTime == start);

            if (clash)
            {
                ViewBag.Error = "That slot is already booked. Pick another time.";
                return View();
            }

            if (db.Appointments.Any(a => a.ClientId == CurrentUserId && a.Status != "Cancelled" && a.StartTime == start))
            {
                ViewBag.Error = "You already have an appointment at that time.";
                return View();
            }

            var appt = new Appointment
            {
                ClientId = CurrentUserId,
                TrainerId = trainerId,
                ProviderId = providerId,
                StartTime = start,
                EndTime = start.AddHours(1),
                Purpose = string.IsNullOrWhiteSpace(purpose)
                    ? (trainerId.HasValue ? "Training session" : "Consultation")
                    : purpose.Trim(),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            db.Appointments.Add(appt);

            if (trainerId.HasValue)
            {
                Notify(trainerId.Value, "Appointment", "New appointment request",
                    "A client requested a session on " + SettingsService.ToDisplayTime(start).ToString("dd MMM yyyy HH:mm") + ".",
                    null, true);
            }

            db.SaveChanges();

            Notify(CurrentUserId, "Appointment", "Appointment requested",
                "Your appointment on " + SettingsService.ToDisplayTime(start).ToString("dd MMM yyyy HH:mm") +
                " is pending confirmation.", appt.AppointmentId, true);

            Audit("AppointmentBooked", "Appointments", appt.AppointmentId);
            db.SaveChanges();

            TempData["Success"] = "Appointment requested. Waiting for confirmation.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SetStatus(int id, string status)
        {
            if (!Statuses.Contains(status) || status == "Pending")
            {
                TempData["Error"] = "Invalid status.";
                return RedirectToAction("Index");
            }

            var appt = db.Appointments.FirstOrDefault(a => a.AppointmentId == id);
            if (appt == null) return HttpNotFound();

            if (User.IsInRole("Client"))
            {
                if (appt.ClientId != CurrentUserId || status != "Cancelled")
                    return new HttpStatusCodeResult(403);
                int cancelWindowHours = SettingsService.Current.CancellationWindowHours;
                if (cancelWindowHours > 0 && appt.StartTime < DateTime.Now.AddHours(cancelWindowHours))
                {
                    TempData["Error"] = "Appointments must be cancelled at least " + cancelWindowHours
                        + " hours before the start time.";
                    return RedirectToAction("Index");
                }
            }
            else if (User.IsInRole("Trainer"))
            {
                if (appt.TrainerId != CurrentUserId) return new HttpStatusCodeResult(403);
            }
            else if (!User.IsInRole("Admin"))
            {
                return new HttpStatusCodeResult(403);
            }
            

            appt.Status = status;

            int? target = User.IsInRole("Client") ? appt.TrainerId : (int?)appt.ClientId;
            if (target.HasValue)
            {
                Notify(target.Value, "Appointment", "Appointment " + status.ToLowerInvariant(),
                    "Your appointment on " + SettingsService.ToDisplayTime(appt.StartTime).ToString("dd MMM yyyy HH:mm") + " is " + status.ToLowerInvariant() + ".",
                    appt.AppointmentId, true);
            }

            Audit("Appointment" + status, "Appointments", id);
            db.SaveChanges();

            TempData["Success"] = "Appointment " + status.ToLowerInvariant() + ".";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var appt = db.Appointments.FirstOrDefault(a => a.AppointmentId == id);
            if (appt == null) return HttpNotFound();

            bool allowed = User.IsInRole("Admin")
                || (User.IsInRole("Trainer") && appt.TrainerId == CurrentUserId && appt.Status == "Cancelled")
                || (User.IsInRole("Client") && appt.ClientId == CurrentUserId && appt.Status == "Cancelled");

            if (!allowed) return new HttpStatusCodeResult(403);

            db.Appointments.Remove(appt);
            Audit("AppointmentDeleted", "Appointments", id);
            db.SaveChanges();

            TempData["Success"] = "Appointment deleted.";
            return RedirectToAction("Index");
        }

        private void LoadTrainers(int? selected = null)
        {
            var trainerIds = db.TrainerClients
                .Where(t => t.ClientId == CurrentUserId && t.Status == "Active")
                .Select(t => t.TrainerId);

            var trainers = db.Users
                .Where(u => trainerIds.Contains(u.UserId) && u.Role.RoleName == "Trainer" && u.IsActive)
                .OrderBy(u => u.FullName).ToList();

            ViewBag.HasTrainer = trainers.Count > 0;
            ViewBag.Trainers = new SelectList(trainers, "UserId", "FullName", selected);
        }

        private bool HasActiveTrainer()
        {
            return db.TrainerClients.Any(t =>
                t.ClientId == CurrentUserId && t.Status == "Active" && t.Trainer.IsActive);
        }
        private void LoadProviders(int? selected = null)
        {
            ViewBag.ProviderList = db.HealthcareProviders
                .Where(p => p.IsActive)
                .OrderBy(p => p.FullName)
                .ToList();

            ViewBag.SelectedProviderId = selected;
        }
    }
}