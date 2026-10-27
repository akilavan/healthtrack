using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using coursework_2.Models;

namespace coursework_2.Controllers
{
    public class ProgressController : BaseController
    {
        [Authorize(Roles = "Client")]
        public ActionResult Index()
        {
            ViewBag.Title = "My Progress";
            ViewBag.Subtitle = "Health";
            ViewBag.IsOwner = true;
            ViewBag.ClientName = CurrentUser != null ? CurrentUser.FullName : "";

            return View("List", LoadMetrics(CurrentUserId));
        }

        [Authorize(Roles = "Trainer,Admin")]
        public ActionResult Client(int id)
        {
            if (User.IsInRole("Trainer"))
            {
                bool assigned = db.TrainerClients.Any(t => t.TrainerId == CurrentUserId && t.ClientId == id && t.Status == "Active");
                if (!assigned) return new HttpStatusCodeResult(403);
            }

            var user = db.Users.FirstOrDefault(u => u.UserId == id && u.Role.RoleName == "Client");
            if (user == null) return HttpNotFound();

            ViewBag.Title = user.FullName + " - Progress";
            ViewBag.Subtitle = "Progress";
            ViewBag.IsOwner = false;
            ViewBag.ClientName = user.FullName;

            return View("List", LoadMetrics(id));
        }

        [Authorize(Roles = "Client")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Add(decimal weightKg, decimal? bodyFatPct, short? systolicBP, short? diastolicBP,
            short? restingHR, decimal? bloodSugar, DateTime? measuredAt, string notes)
        {
            if (weightKg <= 0 || weightKg > 500)
            {
                TempData["Error"] = "Enter a valid weight.";
                return RedirectToAction("Index");
            }

            var profile = db.ClientProfiles.FirstOrDefault(p => p.UserId == CurrentUserId);

            decimal? bmi = null;
            if (profile != null && profile.HeightCm > 0)
            {
                decimal m = profile.HeightCm / 100m;
                bmi = Math.Round(weightKg / (m * m), 1);
            }

            db.HealthMetrics.Add(new HealthMetric
            {
                ClientId = CurrentUserId,
                MeasuredAt = measuredAt.HasValue ? measuredAt.Value : DateTime.Now,
                WeightKg = weightKg,
                BMI = bmi,
                BodyFatPct = bodyFatPct,
                SystolicBP = systolicBP,
                DiastolicBP = diastolicBP,
                RestingHR = restingHR,
                BloodSugar = bloodSugar,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
            });

            db.SaveChanges();
            Audit("HealthMetricAdded", "HealthMetrics");
            db.SaveChanges();

            TempData["Success"] = "Measurement saved.";
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Client")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var metric = db.HealthMetrics.FirstOrDefault(m => m.MetricId == id && m.ClientId == CurrentUserId);
            if (metric == null) return HttpNotFound();

            db.HealthMetrics.Remove(metric);
            Audit("HealthMetricDeleted", "HealthMetrics", id);
            db.SaveChanges();

            TempData["Success"] = "Measurement deleted.";
            return RedirectToAction("Index");
        }

        private List<HealthMetric> LoadMetrics(int clientId)
        {
            var list = db.HealthMetrics
                .Where(m => m.ClientId == clientId)
                .OrderByDescending(m => m.MeasuredAt)
                .ToList();

            var ordered = list.OrderBy(m => m.MeasuredAt).ToList();

            ViewBag.ChartLabels = string.Join(",", ordered.Select(m => m.MeasuredAt.ToString("dd MMM")));
            ViewBag.ChartWeights = string.Join(",", ordered.Select(m => m.WeightKg.ToString("0.0")));

            return list;
        }
    }
}