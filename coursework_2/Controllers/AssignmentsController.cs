using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using coursework_2.Models;

namespace coursework_2.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AssignmentsController : BaseController
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Trainer Assignments";
            ViewBag.Subtitle = "Administration";

            ViewBag.Trainers = db.Users
                .Where(u => u.Role.RoleName == "Trainer" && u.IsActive)
                .OrderBy(u => u.FullName).ToList();

            ViewBag.Clients = db.Users
                .Where(u => u.Role.RoleName == "Client" && u.IsActive)
                .OrderBy(u => u.FullName).ToList();

            var list = db.TrainerClients
                .Include("Trainer").Include("Client").Include("AssignedByUser")
                .OrderByDescending(t => t.AssignedAt)
                .ToList();

            var pendingIds = db.Notifications
                .Where(n => n.Type == "TrainerRequest" && !n.IsRead && n.RelatedId.HasValue)
                .Select(n => n.RelatedId.Value)
                .Distinct()
                .ToList();

            ViewBag.PendingRequests = db.Users
                .Where(u => pendingIds.Contains(u.UserId) && u.Role.RoleName == "Client")
                .OrderBy(u => u.FullName).ToList();

            ViewBag.RequestMessages = db.Notifications
                .Where(n => n.Type == "TrainerRequest" && !n.IsRead && n.RelatedId.HasValue)
                .OrderByDescending(n => n.ScheduledFor)
                .ToList()
                .GroupBy(n => n.RelatedId.Value)
                .ToDictionary(g => g.Key, g => g.First().Message);

            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(int trainerId, int clientId, string status)
        {
            if (!db.Users.Any(u => u.UserId == trainerId && u.Role.RoleName == "Trainer") ||
                !db.Users.Any(u => u.UserId == clientId && u.Role.RoleName == "Client"))
            {
                TempData["Error"] = "Select a valid trainer and client.";
                return RedirectToAction("Index");
            }

            if (db.TrainerClients.Any(t => t.TrainerId == trainerId && t.ClientId == clientId))
            {
                TempData["Error"] = "That client is already assigned to this trainer.";
                return RedirectToAction("Index");
            }

            if (status != "Active" && status != "Inactive") status = "Active";

            db.TrainerClients.Add(new TrainerClient
            {
                TrainerId = trainerId,
                ClientId = clientId,
                AssignedBy = CurrentUserId,
                AssignedAt = DateTime.UtcNow,
                Status = status
            });

            Notify(trainerId, "Assignment", "New client assigned", "A client has been assigned to you.");
            Notify(clientId, "Assignment", "Trainer assigned", "A trainer has been assigned to your account.");

            foreach (var req in db.Notifications
                .Where(n => n.Type == "TrainerRequest" && n.RelatedId == clientId && !n.IsRead)
                .ToList())
            {
                req.IsRead = true;
            }

            db.SaveChanges();
            Audit("ClientAssigned", "TrainerClients");
            db.SaveChanges();

            TempData["Success"] = "Client assigned.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var assignment = db.TrainerClients.FirstOrDefault(t => t.AssignmentId == id);
            if (assignment == null) return HttpNotFound();

            db.TrainerClients.Remove(assignment);
            Audit("AssignmentRemoved", "TrainerClients", id);
            db.SaveChanges();

            TempData["Success"] = "Assignment removed.";
            return RedirectToAction("Index");
        }
    }
}