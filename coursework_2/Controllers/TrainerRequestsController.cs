using System;
using System.Linq;
using System.Web.Mvc;
using coursework_2.Models;

namespace coursework_2.Controllers
{
    [Authorize(Roles = "Client")]
    public class TrainerRequestsController : BaseController
    {
        public ActionResult RequestTrainer()
        {
            ViewBag.Title = "Request Trainer";
            ViewBag.Subtitle = "Coaching";

            int uid = CurrentUserId;

            var active = db.TrainerClients
                .Include("Trainer")
                .FirstOrDefault(t => t.ClientId == uid && t.Status == "Active");

            ViewBag.ActiveAssignment = active;

            bool pending = db.Notifications.Any(n =>
                n.Type == "TrainerRequest" && n.RelatedId == uid && !n.IsRead);

            ViewBag.Pending = pending;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RequestTrainer(string message)
        {
            ViewBag.Title = "Request Trainer";
            ViewBag.Subtitle = "Coaching";

            int uid = CurrentUserId;

            bool hasActive = db.TrainerClients.Any(t => t.ClientId == uid && t.Status == "Active");
            if (hasActive)
            {
                TempData["Error"] = "You already have an assigned trainer.";
                return RedirectToAction("RequestTrainer");
            }

            bool pending = db.Notifications.Any(n =>
                n.Type == "TrainerRequest" && n.RelatedId == uid && !n.IsRead);
            if (pending)
            {
                TempData["Error"] = "Your request is already pending. Please wait for the administrator.";
                return RedirectToAction("RequestTrainer");
            }

            message = (message ?? "").Trim();
            if (message.Length > 500) message = message.Substring(0, 500);

            var client = db.Users.FirstOrDefault(u => u.UserId == uid);
            string clientName = client != null ? client.FullName : "A client";

            var admins = db.Users.Where(u => u.Role.RoleName == "Admin").ToList();
            if (admins.Count == 0)
            {
                TempData["Error"] = "No administrator is available to handle the request.";
                return RedirectToAction("RequestTrainer");
            }

            foreach (var admin in admins)
            {
                Notify(admin.UserId, "TrainerRequest", "Trainer assignment requested",
                    clientName + " requested a trainer assignment." +
                    (string.IsNullOrWhiteSpace(message) ? "" : " Message: " + message),
                    uid);
            }

            db.SaveChanges();
            Audit("TrainerRequested", "TrainerClients", uid);
            db.SaveChanges();

            TempData["Success"] = "Your request has been sent to the administrator.";
            return RedirectToAction("RequestTrainer");
        }
    }
}
