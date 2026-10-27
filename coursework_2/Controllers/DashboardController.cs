using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using coursework_2.Models;

namespace coursework_2.Controllers
{
    public class DashboardController : BaseController
    {
        public ActionResult Index()
        {
            if (User.IsInRole("Admin")) return RedirectToAction("Admin");
            if (User.IsInRole("Trainer")) return RedirectToAction("Trainer");
            return RedirectToAction("Client");
        }

        // ================= CLIENT =================

        [Authorize(Roles = "Client")]
        public ActionResult Client()
        {
            ViewBag.Title = "My Dashboard";
            ViewBag.Subtitle = "Overview";

            int uid = CurrentUserId;
            DateTime now = DateTime.Now;
            DateTime today = DateTime.Today;
            DateTime weekStart = today.AddDays(-6);

            ViewBag.ActivePlan = db.WorkoutPlans.Include("Trainer").Include("Items")
                .Where(p => p.ClientId == uid && p.Status == "Active")
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefault();

            ViewBag.ActiveDiet = db.DietPlans.Include("Trainer")
                .Where(d => d.ClientId == uid && d.Status == "Active")
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefault();

            ViewBag.NextAppointment = db.Appointments.Include("Trainer")
                .Where(a => a.ClientId == uid && a.StartTime >= now && a.Status != "Cancelled")
                .OrderBy(a => a.StartTime)
                .FirstOrDefault();

            ViewBag.LatestMetric = db.HealthMetrics
                .Where(m => m.ClientId == uid)
                .OrderByDescending(m => m.MeasuredAt)
                .FirstOrDefault();

            ViewBag.CaloriesIn = db.MealLogs
                .Where(m => m.ClientId == uid && m.LogDate == today)
                .Select(m => (decimal?)m.Calories).Sum() ?? 0m;

            ViewBag.CaloriesOut = db.WorkoutLogs
                .Where(w => w.ClientId == uid && w.LogDate == today)
                .Select(w => (decimal?)w.CaloriesBurned).Sum() ?? 0m;

            var burned = db.WorkoutLogs
                .Where(w => w.ClientId == uid && w.LogDate >= weekStart)
                .GroupBy(w => w.LogDate)
                .Select(g => new { Day = g.Key, Total = g.Sum(x => x.CaloriesBurned) })
                .ToList();

            var labels = new List<string>();
            var series = new List<string>();
            for (int i = 0; i < 7; i++)
            {
                DateTime d = weekStart.AddDays(i);
                labels.Add(d.ToString("ddd"));
                decimal total = burned.Where(b => b.Day == d)
                                      .Select(b => b.Total ?? 0m)
                                      .FirstOrDefault();
                series.Add(total.ToString("0"));
            }

            ViewBag.ChartLabels = string.Join(",", labels);
            ViewBag.ChartSeries = string.Join(",", series);

            ViewBag.RecentLogs = db.WorkoutLogs.Include("Exercise")
                .Where(w => w.ClientId == uid)
                .OrderByDescending(w => w.LogDate)
                .ThenByDescending(w => w.WorkoutLogId)
                .Take(5)
                .ToList();

            ViewBag.HasActiveTrainer = db.TrainerClients.Any(t => t.ClientId == uid && t.Status == "Active");

            return View();
        }

        // ================= TRAINER =================

        [Authorize(Roles = "Trainer")]
        public ActionResult Trainer()
        {
            ViewBag.Title = "Trainer Dashboard";
            ViewBag.Subtitle = "Overview";

            int tid = CurrentUserId;
            DateTime now = DateTime.Now;
            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);
            DateTime weekStart = today.AddDays(-6);

            var clientIds = db.TrainerClients
                .Where(t => t.TrainerId == tid && t.Status == "Active")
                .Select(t => t.ClientId);

            ViewBag.ClientCount = clientIds.Count();

            ViewBag.TodayAppointments = db.Appointments.Include("Client")
                .Where(a => a.TrainerId == tid && a.StartTime >= today && a.StartTime < tomorrow)
                .OrderBy(a => a.StartTime)
                .ToList();

            ViewBag.Upcoming = db.Appointments.Include("Client")
                .Where(a => a.TrainerId == tid && a.StartTime >= now && a.Status != "Cancelled")
                .OrderBy(a => a.StartTime)
                .Take(5)
                .ToList();

            ViewBag.ActivePlans = db.WorkoutPlans.Count(p => p.TrainerId == tid && p.Status == "Active");
            ViewBag.LogsThisWeek = db.WorkoutLogs
                .Count(w => clientIds.Contains(w.ClientId) && w.LogDate >= weekStart);

            var lastLogs = db.WorkoutLogs
                .Where(w => clientIds.Contains(w.ClientId))
                .GroupBy(w => w.ClientId)
                .Select(g => new { ClientId = g.Key, Last = g.Max(x => x.LogDate) })
                .ToList();

            ViewBag.MyClients = db.Users
                .Where(u => clientIds.Contains(u.UserId))
                .OrderBy(u => u.FullName)
                .Take(8)
                .ToList();

            ViewBag.LastLogDates = lastLogs.ToDictionary(x => x.ClientId, x => x.Last);

            return View();
        }

        // ================= ADMIN =================

        [Authorize(Roles = "Admin")]
        public ActionResult Admin()
        {
            ViewBag.Title = "Admin Dashboard";
            ViewBag.Subtitle = "Overview";

            DateTime now = DateTime.Now;
            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);

            ViewBag.TotalUsers = db.Users.Count();
            ViewBag.TotalClients = db.Users.Count(u => u.Role.RoleName == "Client");
            ViewBag.TotalTrainers = db.Users.Count(u => u.Role.RoleName == "Trainer");
            ViewBag.TodayAppointments = db.Appointments.Count(a => a.StartTime >= today && a.StartTime < tomorrow);
            ViewBag.PendingAppointments = db.Appointments.Count(a => a.Status == "Pending");
            ViewBag.ActivePlans = db.WorkoutPlans.Count(p => p.Status == "Active");
            ViewBag.ActiveAssignments = db.TrainerClients.Count(t => t.Status == "Active");
            ViewBag.TotalAssignments = db.TrainerClients.Count();

            var assignedClients = db.TrainerClients
                .Where(t => t.Status == "Active")
                .Select(t => t.ClientId)
                .ToList();

            ViewBag.UnassignedClients = db.Users.Count(u =>
                u.Role.RoleName == "Client" && !assignedClients.Contains(u.UserId));

            var roleLabels = db.Roles.OrderBy(r => r.RoleName).Select(r => r.RoleName).ToList();
            var roleCounts = db.Roles.OrderBy(r => r.RoleName).Select(r => r.Users.Count()).ToList();

            ViewBag.RoleLabels = string.Join(",", roleLabels);
            ViewBag.RoleCounts = string.Join(",", roleCounts);

            ViewBag.RecentAudits = db.AuditLogs.Include("User")
                .OrderByDescending(a => a.CreatedAt)
                .Take(8)
                .ToList();

            ViewBag.RecentAssignments = db.TrainerClients
                .Include("Trainer").Include("Client")
                .OrderByDescending(t => t.AssignedAt)
                .Take(6)
                .ToList();
            ViewBag.Upcoming = db.Appointments.Include("Client").Include("Trainer")
                .Where(a => a.StartTime >= now && a.Status != "Cancelled")
                .OrderBy(a => a.StartTime)
                .Take(6)
                .ToList();

            return View();
        }
    }
}