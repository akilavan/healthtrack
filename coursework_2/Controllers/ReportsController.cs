using coursework_2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace coursework_2.Controllers
{
    [Authorize(Roles = "Trainer,Admin")]
    public class ReportsController : BaseController
    {
        public ActionResult Index()
        {
            return User.IsInRole("Admin") ? AdminReport() : TrainerReport();
        }

        private ActionResult AdminReport()
        {
            ViewBag.Title = "Reports";
            ViewBag.Subtitle = "Administration";

            DateTime today = DateTime.Today;
            DateTime monthStart = new DateTime(today.Year, today.Month, 1);
            DateTime last30 = today.AddDays(-29);

            ViewBag.TotalUsers = db.Users.Count();
            ViewBag.ActiveUsers = db.Users.Count(u => u.IsActive);
            ViewBag.Clients = db.Users.Count(u => u.Role.RoleName == "Client");
            ViewBag.Trainers = db.Users.Count(u => u.Role.RoleName == "Trainer");
            ViewBag.NewThisMonth = db.Users.Count(u => u.CreatedAt >= monthStart);

            ViewBag.WorkoutLogs30 = db.WorkoutLogs.Count(w => w.LogDate >= last30);
            ViewBag.MealLogs30 = db.MealLogs.Count(m => m.LogDate >= last30);
            ViewBag.CaloriesOut30 = db.WorkoutLogs
                .Where(w => w.LogDate >= last30)
                .Select(w => (decimal?)w.CaloriesBurned).Sum() ?? 0m;
            ViewBag.CaloriesIn30 = db.MealLogs
                .Where(m => m.LogDate >= last30)
                .Select(m => (decimal?)m.Calories).Sum() ?? 0m;

            ViewBag.WorkoutPlans = db.WorkoutPlans.Count();
            ViewBag.DietPlans = db.DietPlans.Count();
            ViewBag.PendingAppointments = db.Appointments.Count(a => a.Status == "Pending");

            var roleCounts = db.Roles
                .OrderBy(r => r.RoleName)
                .Select(r => new { r.RoleName, Count = r.Users.Count() })
                .ToList();

            ViewBag.RoleLabels = string.Join(",", roleCounts.Select(r => r.RoleName));
            ViewBag.RoleSeries = string.Join(",", roleCounts.Select(r => r.Count));

            var apptCounts = db.Appointments
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            ViewBag.ApptLabels = string.Join(",", apptCounts.Select(a => a.Status));
            ViewBag.ApptSeries = string.Join(",", apptCounts.Select(a => a.Count));

            var top = db.WorkoutLogs
                .GroupBy(w => w.ExerciseId)
                .Select(g => new { ExerciseId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            var ids = top.Select(t => t.ExerciseId).ToList();
            var names = db.Exercises.Where(e => ids.Contains(e.ExerciseId))
                .ToDictionary(e => e.ExerciseId, e => e.Name);

            ViewBag.TopExercises = top
                .Select(t => new NamedCount
                {
                    Name = names.ContainsKey(t.ExerciseId) ? names[t.ExerciseId] : "Unknown",
                    Count = t.Count
                })
                .ToList();

            var topTrainers = db.TrainerClients
                .Where(t => t.Status == "Active")
                .GroupBy(t => t.TrainerId)
                .Select(g => new { TrainerId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            var trainerIds = topTrainers.Select(t => t.TrainerId).ToList();
            var trainerNames = db.Users.Where(u => trainerIds.Contains(u.UserId))
                .ToDictionary(u => u.UserId, u => u.FullName);

            ViewBag.TopTrainers = topTrainers
                .Select(t => new NamedCount
                {
                    Name = trainerNames.ContainsKey(t.TrainerId) ? trainerNames[t.TrainerId] : "Unknown",
                    Count = t.Count
                })
                .ToList();

            return View("Index");
        }

        private ActionResult TrainerReport()
        {
            ViewBag.Title = "My Reports";
            ViewBag.Subtitle = "Coaching";

            DateTime today = DateTime.Today;
            DateTime last30 = today.AddDays(-29);

            var clientIds = db.TrainerClients
                .Where(t => t.TrainerId == CurrentUserId && t.Status == "Active")
                .Select(t => t.ClientId)
                .ToList();

            ViewBag.ActiveClients = clientIds.Count;
            ViewBag.TotalWorkoutPlans = db.WorkoutPlans.Count(p => p.TrainerId == CurrentUserId);
            ViewBag.TotalDietPlans = db.DietPlans.Count(p => p.TrainerId == CurrentUserId);
            ViewBag.FeedbackGiven = db.TrainerFeedback.Count(f => f.TrainerId == CurrentUserId);
            ViewBag.PendingAppointments = db.Appointments
                .Count(a => a.TrainerId == CurrentUserId && a.Status == "Pending");

            ViewBag.WorkoutLogs30 = db.WorkoutLogs
                .Count(w => clientIds.Contains(w.ClientId) && w.LogDate >= last30);
            ViewBag.MealLogs30 = db.MealLogs
                .Count(m => clientIds.Contains(m.ClientId) && m.LogDate >= last30);
            ViewBag.CaloriesOut30 = db.WorkoutLogs
                .Where(w => clientIds.Contains(w.ClientId) && w.LogDate >= last30)
                .Select(w => (decimal?)w.CaloriesBurned).Sum() ?? 0m;

            var clients = db.Users
                .Where(u => clientIds.Contains(u.UserId))
                .OrderBy(u => u.FullName)
                .ToList();

            var rows = new List<TrainerClientRow>();

            foreach (var c in clients)
            {
                var metric = db.HealthMetrics
                    .Where(h => h.ClientId == c.UserId)
                    .OrderByDescending(h => h.MeasuredAt)
                    .Select(h => new { h.WeightKg, h.BMI })
                    .FirstOrDefault();

                rows.Add(new TrainerClientRow
                {
                    ClientId = c.UserId,
                    FullName = c.FullName,
                    Email = c.Email,
                    Plans = db.WorkoutPlans.Count(p => p.ClientId == c.UserId && p.TrainerId == CurrentUserId),
                    WorkoutLogs30 = db.WorkoutLogs.Count(w => w.ClientId == c.UserId && w.LogDate >= last30),
                    CaloriesBurned30 = db.WorkoutLogs
                        .Where(w => w.ClientId == c.UserId && w.LogDate >= last30)
                        .Select(w => (decimal?)w.CaloriesBurned).Sum() ?? 0m,
                    LastWorkout = db.WorkoutLogs
                        .Where(w => w.ClientId == c.UserId)
                        .OrderByDescending(w => w.LogDate)
                        .Select(w => (DateTime?)w.LogDate)
                        .FirstOrDefault(),
                    LatestWeight = metric != null ? (decimal?)metric.WeightKg : null,
                    LatestBmi = metric != null ? metric.BMI : null
                });
            }

            ViewBag.Rows = rows;
            ViewBag.ClientLabels = string.Join(",", rows.Select(r => r.FullName.Replace("\"", "")));
            ViewBag.ClientSeries = string.Join(",", rows.Select(r => r.WorkoutLogs30));

            return View("Trainer");
        }
        // ================= CLIENT PERFORMANCE =================

        public ActionResult ClientPerformance(int? clientId, DateTime? from, DateTime? to)
        {
            DateTime fromDate, toDate, toExclusive;
            ResolveRange(from, to, out fromDate, out toDate, out toExclusive);

            var clients = AllowedClients();
            if (clientId.HasValue && !clients.Any(c => c.UserId == clientId.Value))
                return new HttpStatusCodeResult(403);

            ViewBag.Title = "Client Performance";
            ViewBag.Subtitle = "Reports";
            ViewBag.FromIso = fromDate.ToString("yyyy-MM-dd");
            ViewBag.ToIso = toDate.ToString("yyyy-MM-dd");
            ViewBag.ClientId = clientId;
            ViewBag.ClientList = new SelectList(AllowedClients(), "UserId", "FullName", clientId);

            var filtered = clientId.HasValue
                ? clients.Where(c => c.UserId == clientId.Value).ToList()
                : clients;

            var rows = BuildPerformanceRows(fromDate, toExclusive, filtered);

            ViewBag.Rows = rows;
            ViewBag.TotalWorkouts = rows.Sum(r => r.Workouts);
            ViewBag.TotalMinutes = rows.Sum(r => r.Minutes);
            ViewBag.TotalCalories = rows.Sum(r => r.CaloriesOut);
            ViewBag.ActiveClients = rows.Count(r => r.Workouts > 0 || r.Meals > 0);
            ViewBag.ChartClient = null;

            if (clientId.HasValue)
            {
                var trend = db.HealthMetrics
                    .Where(h => h.ClientId == clientId.Value && h.MeasuredAt >= fromDate && h.MeasuredAt < toExclusive)
                    .OrderBy(h => h.MeasuredAt)
                    .Select(h => new { h.MeasuredAt, h.WeightKg })
                    .ToList();

                ViewBag.ChartClient = clients.First(c => c.UserId == clientId.Value).FullName;
                ViewBag.ChartLabels = string.Join(",", trend.Select(t => t.MeasuredAt.ToString("dd MMM")));
                ViewBag.ChartSeries = string.Join(",", trend.Select(t => t.WeightKg.ToString("0.0")));
            }
            else
            {
                var top = rows.Where(r => r.Workouts > 0)
                    .OrderByDescending(r => r.Workouts).Take(8).ToList();

                ViewBag.ChartLabels = string.Join(",", top.Select(r => r.FullName.Replace("\"", "")));
                ViewBag.ChartSeries = string.Join(",", top.Select(r => r.Workouts));
            }

            if (string.IsNullOrEmpty(ViewBag.ChartLabels))
            {
                ViewBag.ChartLabels = "No data";
                ViewBag.ChartSeries = "0";
            }

            return View();
        }

        public ActionResult ClientPerformanceCsv(int? clientId, DateTime? from, DateTime? to)
        {
            DateTime fromDate, toDate, toExclusive;
            ResolveRange(from, to, out fromDate, out toDate, out toExclusive);

            var clients = AllowedClients();
            if (clientId.HasValue && !clients.Any(c => c.UserId == clientId.Value))
                return new HttpStatusCodeResult(403);

            if (clientId.HasValue)
                clients = clients.Where(c => c.UserId == clientId.Value).ToList();

            var rows = BuildPerformanceRows(fromDate, toExclusive, clients);

            var sb = new StringBuilder();
            sb.AppendLine("Client,Email,Workouts,Minutes,Calories burned,Meals,Calories consumed,Start weight,End weight,Weight change,BMI,Appointments completed,Appointments total,Last activity");

            foreach (var r in rows)
            {
                sb.AppendLine(string.Join(",",
                    Csv(r.FullName), Csv(r.Email),
                    r.Workouts.ToString(), r.Minutes.ToString(), r.CaloriesOut.ToString("0"),
                    r.Meals.ToString(), r.CaloriesIn.ToString("0"),
                    Fmt(r.StartWeight), Fmt(r.EndWeight), Fmt(r.WeightChange), Fmt(r.LatestBmi),
                    r.AppointmentsCompleted.ToString(), r.AppointmentsTotal.ToString(),
                    r.LastActivity.HasValue ? r.LastActivity.Value.ToString("yyyy-MM-dd") : ""));
            }

            return CsvResult(sb, "client-performance", fromDate, toDate);
        }

        private List<ClientPerformanceRow> BuildPerformanceRows(DateTime fromDate, DateTime toExclusive, List<User> clients)
        {
            var rows = new List<ClientPerformanceRow>();
            if (clients.Count == 0) return rows;

            var ids = clients.Select(c => c.UserId).ToList();

            var logs = db.WorkoutLogs
                .Where(w => ids.Contains(w.ClientId) && w.LogDate >= fromDate && w.LogDate < toExclusive)
                .Select(w => new { w.ClientId, w.DurationMin, w.CaloriesBurned })
                .ToList();

            var meals = db.MealLogs
                .Where(m => ids.Contains(m.ClientId) && m.LogDate >= fromDate && m.LogDate < toExclusive)
                .Select(m => new { m.ClientId, m.Calories })
                .ToList();

            var metrics = db.HealthMetrics
                .Where(h => ids.Contains(h.ClientId) && h.MeasuredAt >= fromDate && h.MeasuredAt < toExclusive)
                .OrderBy(h => h.MeasuredAt)
                .Select(h => new { h.ClientId, h.WeightKg, h.BMI, h.MeasuredAt })
                .ToList();

            var appts = db.Appointments
                .Where(a => ids.Contains(a.ClientId) && a.StartTime >= fromDate && a.StartTime < toExclusive)
                .Select(a => new { a.ClientId, a.Status })
                .ToList();

            var lastWorkouts = db.WorkoutLogs
                .Where(w => ids.Contains(w.ClientId))
                .GroupBy(w => w.ClientId)
                .Select(g => new { ClientId = g.Key, Last = g.Max(x => x.LogDate) })
                .ToList()
                .ToDictionary(x => x.ClientId, x => (DateTime?)x.Last);

            var lastMeals = db.MealLogs
                .Where(m => ids.Contains(m.ClientId))
                .GroupBy(m => m.ClientId)
                .Select(g => new { ClientId = g.Key, Last = g.Max(x => x.LogDate) })
                .ToList()
                .ToDictionary(x => x.ClientId, x => (DateTime?)x.Last);

            foreach (var c in clients)
            {
                var cl = logs.Where(x => x.ClientId == c.UserId).ToList();
                var cm = meals.Where(x => x.ClientId == c.UserId).ToList();
                var cq = metrics.Where(x => x.ClientId == c.UserId).ToList();
                var ca = appts.Where(x => x.ClientId == c.UserId).ToList();

                DateTime? last = null;
                DateTime? lw, lm;
                if (lastWorkouts.TryGetValue(c.UserId, out lw)) last = lw;
                if (lastMeals.TryGetValue(c.UserId, out lm) && (!last.HasValue || lm > last.Value)) last = lm;

                rows.Add(new ClientPerformanceRow
                {
                    ClientId = c.UserId,
                    FullName = c.FullName,
                    Email = c.Email,
                    Workouts = cl.Count,
                    Minutes = cl.Sum(x => (int?)x.DurationMin) ?? 0,
                    CaloriesOut = cl.Sum(x => (decimal?)x.CaloriesBurned) ?? 0m,
                    Meals = cm.Count,
                    CaloriesIn = cm.Sum(x => (decimal?)x.Calories) ?? 0m,
                    StartWeight = cq.Count > 0 ? (decimal?)cq.First().WeightKg : null,
                    EndWeight = cq.Count > 0 ? (decimal?)cq.Last().WeightKg : null,
                    LatestBmi = cq.Count > 0 ? cq.Last().BMI : null,
                    AppointmentsCompleted = ca.Count(a => a.Status == "Completed"),
                    AppointmentsTotal = ca.Count,
                    LastActivity = last
                });
            }

            return rows;
        }

        // ================= POPULAR ROUTINES =================

        public ActionResult PopularRoutines(DateTime? from, DateTime? to)
        {
            DateTime fromDate, toDate, toExclusive;
            ResolveRange(from, to, out fromDate, out toDate, out toExclusive);

            ViewBag.Title = "Popular Routines";
            ViewBag.Subtitle = "Reports";
            ViewBag.FromIso = fromDate.ToString("yyyy-MM-dd");
            ViewBag.ToIso = toDate.ToString("yyyy-MM-dd");

            var usage = BuildExerciseUsage(fromDate, toExclusive);
            var plans = BuildPlanUsage(fromDate, toExclusive);

            ViewBag.Usage = usage;
            ViewBag.Plans = plans;
            ViewBag.TotalLogs = usage.Sum(u => u.Logs);
            ViewBag.DistinctExercises = usage.Count;
            ViewBag.PlansUsed = plans.Count;
            ViewBag.TopCategory = usage.GroupBy(u => u.Category)
                .OrderByDescending(g => g.Sum(x => x.Logs))
                .Select(g => g.Key)
                .FirstOrDefault() ?? "-";

            ViewBag.Categories = usage.GroupBy(u => u.Category)
                .Select(g => new NamedCount { Name = g.Key, Count = g.Sum(x => x.Logs) })
                .OrderByDescending(x => x.Count)
                .ToList();

            var top = usage.Take(8).ToList();
            ViewBag.ChartLabels = string.Join(",", top.Select(u => u.Name.Replace("\"", "")));
            ViewBag.ChartSeries = string.Join(",", top.Select(u => u.Logs));

            if (string.IsNullOrEmpty(ViewBag.ChartLabels))
            {
                ViewBag.ChartLabels = "No data";
                ViewBag.ChartSeries = "0";
            }

            return View();
        }

        public ActionResult PopularRoutinesCsv(DateTime? from, DateTime? to)
        {
            DateTime fromDate, toDate, toExclusive;
            ResolveRange(from, to, out fromDate, out toDate, out toExclusive);

            var usage = BuildExerciseUsage(fromDate, toExclusive);
            var plans = BuildPlanUsage(fromDate, toExclusive);

            var sb = new StringBuilder();
            sb.AppendLine("Exercise,Category,Times logged,Minutes,Calories burned");
            foreach (var u in usage)
            {
                sb.AppendLine(string.Join(",",
                    Csv(u.Name), Csv(u.Category), u.Logs.ToString(),
                    u.Minutes.ToString(), u.Calories.ToString("0")));
            }

            sb.AppendLine();
            sb.AppendLine("Workout plan,Times logged");
            foreach (var p in plans)
                sb.AppendLine(string.Join(",", Csv(p.Title), p.Logs.ToString()));

            return CsvResult(sb, "popular-routines", fromDate, toDate);
        }

        private List<ExerciseUsageRow> BuildExerciseUsage(DateTime fromDate, DateTime toExclusive)
        {
            var ids = VisibleClientIds();
            if (ids.Count == 0) return new List<ExerciseUsageRow>();

            var logs = db.WorkoutLogs
                .Where(w => ids.Contains(w.ClientId) && w.LogDate >= fromDate && w.LogDate < toExclusive)
                .Select(w => new { w.ExerciseId, w.DurationMin, w.CaloriesBurned })
                .ToList();

            var exercises = db.Exercises.ToDictionary(e => e.ExerciseId, e => new { e.Name, e.Category });

            return logs.GroupBy(l => l.ExerciseId)
                .Select(g =>
                {
                    if (exercises.ContainsKey(g.Key))
                    {
                        var ex = exercises[g.Key];
                        return new ExerciseUsageRow
                        {
                            Name = ex.Name,
                            Category = ex.Category ?? "Other",
                            Logs = g.Count(),
                            Minutes = g.Sum(x => (int?)x.DurationMin) ?? 0,
                            Calories = g.Sum(x => (decimal?)x.CaloriesBurned) ?? 0m
                        };
                    }

                    return new ExerciseUsageRow
                    {
                        Name = "Unknown",
                        Category = "Other",
                        Logs = g.Count(),
                        Minutes = g.Sum(x => (int?)x.DurationMin) ?? 0,
                        Calories = g.Sum(x => (decimal?)x.CaloriesBurned) ?? 0m
                    };
                })
                .OrderByDescending(r => r.Logs)
                .ToList();
        }

        private List<PlanUsageRow> BuildPlanUsage(DateTime fromDate, DateTime toExclusive)
        {
            var ids = VisibleClientIds();
            if (ids.Count == 0) return new List<PlanUsageRow>();

            var rows = (from w in db.WorkoutLogs
                        join i in db.WorkoutPlanItems on w.PlanItemId equals (int?)i.PlanItemId
                        join p in db.WorkoutPlans on i.PlanId equals p.PlanId
                        where ids.Contains(w.ClientId)
                              && w.LogDate >= fromDate && w.LogDate < toExclusive
                        select new { p.Title })
                       .ToList();

            return rows.GroupBy(r => r.Title)
                .Select(g => new PlanUsageRow { Title = g.Key, Logs = g.Count() })
                .OrderByDescending(r => r.Logs)
                .Take(10)
                .ToList();
        }

        // ================= ENGAGEMENT =================

        public ActionResult Engagement(DateTime? from, DateTime? to)
        {
            DateTime fromDate, toDate, toExclusive;
            ResolveRange(from, to, out fromDate, out toDate, out toExclusive);

            ViewBag.Title = "Engagement";
            ViewBag.Subtitle = "Reports";
            ViewBag.FromIso = fromDate.ToString("yyyy-MM-dd");
            ViewBag.ToIso = toDate.ToString("yyyy-MM-dd");

            var rows = BuildEngagementRows(fromDate, toExclusive);
            ViewBag.Rows = rows;
            ViewBag.TotalClients = rows.Count;
            ViewBag.ActiveClients = rows.Count(r => r.Workouts > 0 || r.Meals > 0);
            ViewBag.TotalWorkouts = rows.Sum(r => r.Workouts);
            ViewBag.TotalMeals = rows.Sum(r => r.Meals);
            ViewBag.AveragePerClient = rows.Count > 0
                ? Math.Round((double)rows.Sum(r => r.Workouts + r.Meals) / rows.Count, 1)
                : 0;

            var ids = VisibleClientIds();

            var apptCounts = new List<NamedCount>();
            if (ids.Count > 0)
            {
                var counts = db.Appointments
                    .Where(a => ids.Contains(a.ClientId) && a.StartTime >= fromDate && a.StartTime < toExclusive)
                    .GroupBy(a => a.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToList();

                apptCounts = counts.Select(x => new NamedCount { Name = x.Status, Count = x.Count }).ToList();
            }

            ViewBag.ApptLabels = apptCounts.Count > 0 ? string.Join(",", apptCounts.Select(a => a.Name)) : "No data";
            ViewBag.ApptSeries = apptCounts.Count > 0 ? string.Join(",", apptCounts.Select(a => a.Count)) : "0";

            var dates = new List<DateTime>();
            if (ids.Count > 0)
            {
                dates = db.WorkoutLogs
                    .Where(w => ids.Contains(w.ClientId) && w.LogDate >= fromDate && w.LogDate < toExclusive)
                    .Select(w => w.LogDate)
                    .ToList();
            }

            var trend = new List<NamedCount>();
            int totalDays = (int)(toDate - fromDate).TotalDays + 1;

            if (totalDays <= 62)
            {
                for (var d = fromDate; d <= toDate; d = d.AddDays(1))
                {
                    trend.Add(new NamedCount
                    {
                        Name = d.ToString("dd MMM"),
                        Count = dates.Count(x => x.Date == d.Date)
                    });
                }
            }
            else
            {
                trend = dates.GroupBy(x => new DateTime(x.Year, x.Month, 1))
                    .OrderBy(g => g.Key)
                    .Select(g => new NamedCount { Name = g.Key.ToString("MMM yyyy"), Count = g.Count() })
                    .ToList();
            }

            ViewBag.TrendLabels = trend.Count > 0 ? string.Join(",", trend.Select(t => t.Name)) : "No data";
            ViewBag.TrendSeries = trend.Count > 0 ? string.Join(",", trend.Select(t => t.Count)) : "0";

            return View();
        }

        public ActionResult EngagementCsv(DateTime? from, DateTime? to)
        {
            DateTime fromDate, toDate, toExclusive;
            ResolveRange(from, to, out fromDate, out toDate, out toExclusive);

            var rows = BuildEngagementRows(fromDate, toExclusive);

            var sb = new StringBuilder();
            sb.AppendLine("Client,Email,Workouts,Meals,Last activity,Days since,Status");

            foreach (var r in rows)
            {
                sb.AppendLine(string.Join(",",
                    Csv(r.FullName), Csv(r.Email),
                    r.Workouts.ToString(), r.Meals.ToString(),
                    r.LastActivity.HasValue ? r.LastActivity.Value.ToString("yyyy-MM-dd") : "",
                    r.DaysSince.HasValue ? r.DaysSince.Value.ToString() : "",
                    Csv(r.Status)));
            }

            return CsvResult(sb, "engagement", fromDate, toDate);
        }

        private List<EngagementRow> BuildEngagementRows(DateTime fromDate, DateTime toExclusive)
        {
            var clients = AllowedClients();
            var rows = new List<EngagementRow>();
            if (clients.Count == 0) return rows;

            var ids = clients.Select(c => c.UserId).ToList();

            var logs = db.WorkoutLogs
                .Where(w => ids.Contains(w.ClientId) && w.LogDate >= fromDate && w.LogDate < toExclusive)
                .Select(w => new { w.ClientId, w.LogDate })
                .ToList();

            var meals = db.MealLogs
                .Where(m => ids.Contains(m.ClientId) && m.LogDate >= fromDate && m.LogDate < toExclusive)
                .Select(m => new { m.ClientId, m.LogDate })
                .ToList();

            var lastWorkouts = db.WorkoutLogs
                .Where(w => ids.Contains(w.ClientId))
                .GroupBy(w => w.ClientId)
                .Select(g => new { ClientId = g.Key, Last = g.Max(x => x.LogDate) })
                .ToList()
                .ToDictionary(x => x.ClientId, x => (DateTime?)x.Last);

            var lastMeals = db.MealLogs
                .Where(m => ids.Contains(m.ClientId))
                .GroupBy(m => m.ClientId)
                .Select(g => new { ClientId = g.Key, Last = g.Max(x => x.LogDate) })
                .ToList()
                .ToDictionary(x => x.ClientId, x => (DateTime?)x.Last);

            DateTime today = DateTime.Today;

            foreach (var c in clients)
            {
                DateTime? last = null;
                DateTime? lw, lm;
                if (lastWorkouts.TryGetValue(c.UserId, out lw)) last = lw;
                if (lastMeals.TryGetValue(c.UserId, out lm) && (!last.HasValue || lm > last.Value)) last = lm;

                int? days = last.HasValue ? (int?)(today - last.Value.Date).TotalDays : null;
                string status = !days.HasValue ? "Inactive"
                    : (days.Value <= 7 ? "Active" : (days.Value <= 14 ? "At risk" : "Inactive"));

                rows.Add(new EngagementRow
                {
                    ClientId = c.UserId,
                    FullName = c.FullName,
                    Email = c.Email,
                    Workouts = logs.Count(x => x.ClientId == c.UserId),
                    Meals = meals.Count(x => x.ClientId == c.UserId),
                    LastActivity = last,
                    DaysSince = days,
                    Status = status
                });
            }

            return rows.OrderBy(r => r.FullName).ToList();
        }

        // ================= HELPERS =================

        private void ResolveRange(DateTime? from, DateTime? to, out DateTime fromDate, out DateTime toDate, out DateTime toExclusive)
        {
            fromDate = (from ?? DateTime.Today.AddDays(-29)).Date;
            toDate = (to ?? DateTime.Today).Date;

            if (toDate < fromDate)
            {
                DateTime swap = fromDate;
                fromDate = toDate;
                toDate = swap;
            }

            toExclusive = toDate.AddDays(1);
        }

        private List<int> VisibleClientIds()
        {
            var q = db.Users.Where(u => u.Role.RoleName == "Client").Select(u => u.UserId);

            if (User.IsInRole("Trainer"))
            {
                var assigned = db.TrainerClients
                    .Where(t => t.TrainerId == CurrentUserId && t.Status == "Active")
                    .Select(t => t.ClientId);

                q = q.Where(id => assigned.Contains(id));
            }

            return q.ToList();
        }

        private List<User> AllowedClients()
        {
            var ids = VisibleClientIds();
            return db.Users.Where(u => ids.Contains(u.UserId)).OrderBy(u => u.FullName).ToList();
        }

        private static string Csv(string value)
        {
            value = value ?? "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
                value = "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        private static string Fmt(decimal? value)
        {
            return value.HasValue ? value.Value.ToString("0.0") : "";
        }

        private FileContentResult CsvResult(StringBuilder sb, string name, DateTime fromDate, DateTime toDate)
        {
            var bytes = new UTF8Encoding(true).GetBytes(sb.ToString());
            return File(bytes, "text/csv", name + "-" + fromDate.ToString("yyyyMMdd") + "-" + toDate.ToString("yyyyMMdd") + ".csv");
        }
    }
}