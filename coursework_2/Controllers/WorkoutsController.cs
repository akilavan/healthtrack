using System;
using System.Linq;
using System.Data.Entity;
using System.Web.Mvc;
using coursework_2.Models;

namespace coursework_2.Controllers
{
    public class WorkoutsController : BaseController
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Workout Plans";
            ViewBag.Subtitle = "Fitness";

            if (User.IsInRole("Client"))
            {
                var mine = db.WorkoutPlans.Include("Trainer").Include("Items")
                    .Where(p => p.ClientId == CurrentUserId)
                    .OrderByDescending(p => p.CreatedAt).ToList();
                return View(mine);
            }

            if (User.IsInRole("Trainer"))
            {
                var mine = db.WorkoutPlans.Include("Client").Include("Items")
                    .Where(p => p.TrainerId == CurrentUserId)
                    .OrderByDescending(p => p.CreatedAt).ToList();
                return View(mine);
            }

            var all = db.WorkoutPlans.Include("Client").Include("Trainer").Include("Items")
                .OrderByDescending(p => p.CreatedAt).Take(200).ToList();
            return View(all);
        }

        public ActionResult Details(int id = 0)
        {
            if (id == 0) return RedirectToAction("Index");
            var plan = db.WorkoutPlans
                .Include("Client")
                .Include("Trainer")
                .Include("Items.Exercise")
                .FirstOrDefault(p => p.PlanId == id);

            if (plan == null) return HttpNotFound();
            if (User.IsInRole("Client") && plan.ClientId != CurrentUserId) return new HttpStatusCodeResult(403);
            if (User.IsInRole("Trainer") && plan.TrainerId != CurrentUserId) return new HttpStatusCodeResult(403);

            ViewBag.Title = plan.Title;
            ViewBag.Subtitle = "Workout Plan";
            ViewBag.CanEdit = User.IsInRole("Trainer") && plan.TrainerId == CurrentUserId;

            if ((bool)ViewBag.CanEdit)
                ViewBag.Exercises = db.Exercises.OrderBy(e => e.Name).ToList();

            var itemIds = plan.Items.Select(i => i.PlanItemId).ToList();

            ViewBag.Logs = db.WorkoutLogs.Include("Exercise")
                .Where(w => w.PlanItemId.HasValue && itemIds.Contains(w.PlanItemId.Value))
                .OrderByDescending(w => w.LogDate)
                .ThenByDescending(w => w.WorkoutLogId)
                .Take(20).ToList();

            return View(plan);
        }

        [Authorize(Roles = "Trainer")]
        [HttpGet]
        public ActionResult Form(int? id)
        {
            ViewBag.Title = id.HasValue ? "Edit Workout Plan" : "New Workout Plan";
            ViewBag.Subtitle = "Workout Plans";

            PopulateClients();

            if (!id.HasValue)
                return View(new WorkoutPlan { StartDate = DateTime.Today, Status = "Active" });

            var plan = db.WorkoutPlans.FirstOrDefault(p => p.PlanId == id.Value && p.TrainerId == CurrentUserId);
            if (plan == null) return new HttpStatusCodeResult(403);

            PopulateClients(plan.ClientId);
            return View(plan);
        }

        [Authorize(Roles = "Trainer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Form(int? id, int clientId, string title, string goal,
            DateTime startDate, DateTime? endDate, string status)
        {
            ViewBag.Title = id.HasValue ? "Edit Workout Plan" : "New Workout Plan";
            ViewBag.Subtitle = "Workout Plans";

            var model = new WorkoutPlan
            {
                PlanId = id ?? 0,
                ClientId = clientId,
                Title = (title ?? "").Trim(),
                Goal = (goal ?? "").Trim(),
                StartDate = startDate.Date,
                EndDate = endDate.HasValue ? endDate.Value.Date : (DateTime?)null,
                Status = status
            };

            if (string.IsNullOrWhiteSpace(model.Title) || clientId <= 0)
            {
                ViewBag.Error = "Plan title and client are required.";
                PopulateClients(clientId);
                return View(model);
            }

            if (!db.Users.Any(u => u.UserId == clientId && u.Role.RoleName == "Client"))
            {
                ViewBag.Error = "Select a valid client.";
                PopulateClients(clientId);
                return View(model);
            }

            bool assigned = db.TrainerClients.Any(t =>
                t.TrainerId == CurrentUserId && t.ClientId == clientId && t.Status == "Active");

            if (!assigned)
            {
                ViewBag.Error = "You can only create plans for clients assigned to you.";
                PopulateClients(clientId);
                return View(model);
            }

            if (model.EndDate.HasValue && model.EndDate.Value < model.StartDate)
            {
                ViewBag.Error = "End date cannot be before the start date.";
                PopulateClients(clientId);
                return View(model);
            }

            if (model.Status != "Active" && model.Status != "Completed" && model.Status != "Cancelled")
                model.Status = "Active";

            bool isNew = !id.HasValue;
            WorkoutPlan plan;

            if (isNew)
            {
                plan = new WorkoutPlan { TrainerId = CurrentUserId, CreatedAt = DateTime.UtcNow };
                db.WorkoutPlans.Add(plan);
            }
            else
            {
                plan = db.WorkoutPlans.FirstOrDefault(p => p.PlanId == id.Value && p.TrainerId == CurrentUserId);
                if (plan == null) return new HttpStatusCodeResult(403);
            }

            plan.ClientId = model.ClientId;
            plan.Title = model.Title;
            plan.Goal = model.Goal;
            plan.StartDate = model.StartDate;
            plan.EndDate = model.EndDate;
            plan.Status = model.Status;

            db.SaveChanges();
            Audit(isNew ? "WorkoutPlanCreated" : "WorkoutPlanUpdated", "WorkoutPlans", plan.PlanId);
            db.SaveChanges();

            TempData["Success"] = isNew ? "Plan created. Add exercises below." : "Plan updated.";
            return RedirectToAction("Details", new { id = plan.PlanId });
        }

        [Authorize(Roles = "Trainer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddItem(int planId, int exerciseId, byte dayOfWeek,
            byte? sets, short? reps, short? durationMin, short? restSec, string notes)
        {
            var plan = db.WorkoutPlans.FirstOrDefault(p => p.PlanId == planId && p.TrainerId == CurrentUserId);
            if (plan == null) return new HttpStatusCodeResult(403);

            if (!db.Exercises.Any(e => e.ExerciseId == exerciseId) || dayOfWeek > 6
                || (!sets.HasValue && !reps.HasValue && !durationMin.HasValue))
            {
                TempData["Error"] = "Pick a valid exercise, day and at least sets, reps or duration.";
                return RedirectToAction("Details", new { id = planId });
            }

            db.WorkoutPlanItems.Add(new WorkoutPlanItem
            {
                PlanId = planId,
                ExerciseId = exerciseId,
                DayOfWeek = dayOfWeek,
                Sets = sets,
                Reps = reps,
                DurationMin = durationMin,
                RestSec = restSec,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
            });

            Audit("WorkoutItemAdded", "WorkoutPlanItems", planId);
            db.SaveChanges();

            TempData["Success"] = "Exercise added.";
            return RedirectToAction("Details", new { id = planId });
        }

        [Authorize(Roles = "Trainer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveItem(int id)
        {
            var item = db.WorkoutPlanItems.Include("Plan").FirstOrDefault(i => i.PlanItemId == id);
            if (item == null) return HttpNotFound();
            if (item.Plan.TrainerId != CurrentUserId) return new HttpStatusCodeResult(403);

            int planId = item.PlanId;

            foreach (var log in db.WorkoutLogs.Where(l => l.PlanItemId == id).ToList())
                log.PlanItemId = null;

            db.WorkoutPlanItems.Remove(item);
            Audit("WorkoutItemRemoved", "WorkoutPlanItems", id);
            db.SaveChanges();

            TempData["Success"] = "Exercise removed from plan.";
            return RedirectToAction("Details", new { id = planId });
        }

        [Authorize(Roles = "Client")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogItem(int id)
        {
            var item = db.WorkoutPlanItems.Include("Exercise").Include("Plan")
                .FirstOrDefault(i => i.PlanItemId == id);

            if (item == null) return HttpNotFound();
            if (item.Plan.ClientId != CurrentUserId) return new HttpStatusCodeResult(403);

            DateTime today = DateTime.Today;

            if (db.WorkoutLogs.Any(w => w.ClientId == CurrentUserId && w.PlanItemId == id && w.LogDate == today))
            {
                TempData["Error"] = "You already logged this exercise today.";
                return RedirectToAction("Details", new { id = item.PlanId });
            }

            double totalMinutes = item.DurationMin.HasValue
                ? item.DurationMin.Value
                : (item.Sets.HasValue && item.Reps.HasValue
                    ? (item.Sets.Value * (item.Reps.Value * 3 + (item.RestSec ?? 60))) / 60.0
                    : 0);

            decimal? clientWeight = db.HealthMetrics
                .Where(m => m.ClientId == item.Plan.ClientId)
                .OrderByDescending(m => m.MeasuredAt)
                .Select(m => (decimal?)m.WeightKg)
                .FirstOrDefault();

            decimal weightKg = clientWeight ?? 70m;

            double restSeconds = item.Sets.HasValue && item.Sets.Value > 1
                ? (item.Sets.Value - 1) * (item.RestSec ?? 60)
                : 0;
            double restMinutes = Math.Min(restSeconds / 60.0, totalMinutes);
            double activeMinutes = Math.Max(0, totalMinutes - restMinutes);

            decimal kcalPerMin = item.Exercise.MetValue * 3.5m * weightKg / 200m;
            decimal restKcalPerMin = 1.0m * 3.5m * weightKg / 200m;

            decimal? calories = totalMinutes > 0
                ? Math.Round(kcalPerMin * (decimal)activeMinutes + restKcalPerMin * (decimal)restMinutes, 1)
                : (decimal?)null;

            db.WorkoutLogs.Add(new WorkoutLog
            {
                ClientId = CurrentUserId,
                PlanItemId = item.PlanItemId,
                ExerciseId = item.ExerciseId,
                LogDate = today,
                Sets = item.Sets,
                Reps = item.Reps,
                DurationMin = item.DurationMin,
                CaloriesBurned = calories,
                Notes = "Logged from plan",
                CreatedAt = DateTime.UtcNow
            });

            Audit("WorkoutLogged", "WorkoutLogs", item.PlanId);
            db.SaveChanges();

            TempData["Success"] = "Workout logged.";
            return RedirectToAction("Details", new { id = item.PlanId });
        }

        [Authorize(Roles = "Trainer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var plan = db.WorkoutPlans.Include("Items").FirstOrDefault(p => p.PlanId == id && p.TrainerId == CurrentUserId);
            if (plan == null) return new HttpStatusCodeResult(403);

            var itemIds = plan.Items.Select(i => i.PlanItemId).ToList();

            foreach (var log in db.WorkoutLogs.Where(l => l.PlanItemId.HasValue && itemIds.Contains(l.PlanItemId.Value)).ToList())
                log.PlanItemId = null;

            db.WorkoutPlans.Remove(plan);
            Audit("WorkoutPlanDeleted", "WorkoutPlans", id);
            db.SaveChanges();

            TempData["Success"] = "Plan deleted.";
            return RedirectToAction("Index");
        }

        private void PopulateClients(int? selected = null)
        {
            var ids = db.TrainerClients
                .Where(t => t.TrainerId == CurrentUserId && t.Status == "Active")
                .Select(t => t.ClientId);

            var clients = db.Users
                .Where(u => u.Role.RoleName == "Client" && u.IsActive && ids.Contains(u.UserId))
                .OrderBy(u => u.FullName)
                .ToList();

            ViewBag.HasClients = clients.Count > 0;
            ViewBag.Clients = new SelectList(clients, "UserId", "FullName", selected);
        }
    }
}
