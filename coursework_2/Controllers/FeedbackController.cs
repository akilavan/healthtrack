using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using coursework_2.Models;

namespace coursework_2.Controllers
{
    public class FeedbackController : BaseController
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Trainer Feedback";
            ViewBag.Subtitle = "Coaching";

            if (User.IsInRole("Client"))
            {
                var received = db.TrainerFeedback.Include("Trainer").Include("WorkoutLog")
                    .Where(f => f.ClientId == CurrentUserId)
                    .OrderByDescending(f => f.CreatedAt).ToList();
                return View(received);
            }

            if (User.IsInRole("Trainer"))
            {
                ViewBag.Clients = db.Users
                    .Where(u => u.Role.RoleName == "Client" && u.IsActive &&
                        db.TrainerClients.Any(t => t.TrainerId == CurrentUserId && t.ClientId == u.UserId && t.Status == "Active"))
                    .OrderBy(u => u.FullName).ToList();

                ViewBag.Logs = db.WorkoutLogs.Include("Exercise").Include("Client")
                    .Where(w => db.TrainerClients.Any(t => t.TrainerId == CurrentUserId && t.ClientId == w.ClientId && t.Status == "Active"))
                    .OrderByDescending(w => w.LogDate).Take(100).ToList();

                var given = db.TrainerFeedback.Include("Client").Include("WorkoutLog")
                    .Where(f => f.TrainerId == CurrentUserId)
                    .OrderByDescending(f => f.CreatedAt).ToList();
                return View(given);
            }

            var all = db.TrainerFeedback.Include("Trainer").Include("Client")
                .OrderByDescending(f => f.CreatedAt).Take(200).ToList();
            return View(all);
        }

        [Authorize(Roles = "Trainer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(int clientId, int? workoutLogId, string comment)
        {
            if (!db.Users.Any(u => u.UserId == clientId && u.Role.RoleName == "Client"))
            {
                TempData["Error"] = "Select a valid client.";
                return RedirectToAction("Index");
            }

            bool assigned = db.TrainerClients.Any(t =>
                t.TrainerId == CurrentUserId && t.ClientId == clientId && t.Status == "Active");

            if (!assigned)
            {
                TempData["Error"] = "You can only give feedback to clients assigned to you.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(comment) || comment.Trim().Length < 5)
            {
                TempData["Error"] = "Feedback must be at least 5 characters.";
                return RedirectToAction("Index");
            }

            if (comment.Trim().Length > 500) comment = comment.Trim().Substring(0, 500);

            if (workoutLogId.HasValue && !db.WorkoutLogs.Any(w => w.WorkoutLogId == workoutLogId.Value && w.ClientId == clientId))
                workoutLogId = null;

            db.TrainerFeedback.Add(new TrainerFeedback
            {
                TrainerId = CurrentUserId,
                ClientId = clientId,
                WorkoutLogId = workoutLogId,
                Comment = comment.Trim(),
                CreatedAt = DateTime.UtcNow
            });

            string preview = comment.Trim();
            if (preview.Length > 100) preview = preview.Substring(0, 100) + "...";

            Notify(clientId, "Feedback", "New trainer feedback", "Your trainer left feedback: " + preview);

            db.SaveChanges();
            Audit("FeedbackGiven", "TrainerFeedback");
            db.SaveChanges();

            TempData["Success"] = "Feedback sent.";
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Trainer,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var fb = db.TrainerFeedback.FirstOrDefault(f => f.FeedbackId == id);
            if (fb == null) return HttpNotFound();

            if (User.IsInRole("Trainer") && fb.TrainerId != CurrentUserId)
                return new HttpStatusCodeResult(403);

            db.TrainerFeedback.Remove(fb);
            Audit("FeedbackDeleted", "TrainerFeedback", id);
            db.SaveChanges();

            TempData["Success"] = "Feedback deleted.";
            return RedirectToAction("Index");
        }
    }
}