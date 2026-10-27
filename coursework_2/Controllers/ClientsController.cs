using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using coursework_2.Models;

namespace coursework_2.Controllers
{
    [Authorize(Roles = "Trainer,Admin")]
    public class ClientsController : BaseController
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Clients";
            ViewBag.Subtitle = "Coaching";

            if (User.IsInRole("Admin"))
            {
                var all = db.Users.Include("ClientProfile")
                    .Where(u => u.Role.RoleName == "Client")
                    .OrderBy(u => u.FullName).ToList();
                return View(all);
            }

            var ids = db.TrainerClients
                .Where(t => t.TrainerId == CurrentUserId && t.Status == "Active")
                .Select(t => t.ClientId);

            var mine = db.Users.Include("ClientProfile")
                .Where(u => ids.Contains(u.UserId))
                .OrderBy(u => u.FullName).ToList();

            return View(mine);
        }

        public ActionResult Details(int id = 0)
        {
            if (id == 0) return RedirectToAction("Index");
            var client = db.Users.Include("ClientProfile").Include("Role")
                .FirstOrDefault(u => u.UserId == id && u.Role.RoleName == "Client");

            if (client == null) return HttpNotFound();

            if (User.IsInRole("Trainer"))
            {
                bool assigned = db.TrainerClients.Any(t => t.TrainerId == CurrentUserId && t.ClientId == id && t.Status == "Active");
                if (!assigned) return new HttpStatusCodeResult(403);
            }

            ViewBag.Title = client.FullName;
            ViewBag.Subtitle = "Client";

            ViewBag.LatestMetric = db.HealthMetrics
                .Where(m => m.ClientId == id)
                .OrderByDescending(m => m.MeasuredAt)
                .FirstOrDefault();

            ViewBag.Plans = db.WorkoutPlans.Include("Items")
                .Where(p => p.ClientId == id)
                .OrderByDescending(p => p.CreatedAt).ToList();

            ViewBag.Diets = db.DietPlans
                .Where(d => d.ClientId == id)
                .OrderByDescending(d => d.CreatedAt).ToList();

            ViewBag.Appointments = db.Appointments
                .Where(a => a.ClientId == id)
                .OrderByDescending(a => a.StartTime).Take(10).ToList();

            ViewBag.Trainer = db.TrainerClients.Include("Trainer")
                .Where(t => t.ClientId == id && t.Status == "Active")
                .Select(t => t.Trainer)
                .FirstOrDefault();

            return View(client);
        }
    }
}