using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace coursework_2.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AuditLogsController : BaseController
    {
        public ActionResult Index(string actionName, string search)
        {
            ViewBag.Title = "Audit Logs";
            ViewBag.Subtitle = "Administration";
            ViewBag.Action = actionName;
            ViewBag.Search = search;

            var logs = db.AuditLogs.Include("User").AsQueryable();

            if (!string.IsNullOrWhiteSpace(actionName))
                logs = logs.Where(l => l.Action == actionName);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                logs = logs.Where(l => l.EntityName.Contains(search) || l.IpAddress.Contains(search));
            }

            ViewBag.Actions = db.AuditLogs
                .Select(l => l.Action)
                .Distinct()
                .OrderBy(a => a)
                .ToList();

            return View(logs.OrderByDescending(l => l.CreatedAt).Take(300).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ClearOld(int days = 30)
        {
            if (days < 7) days = 7;
            if (days > 365) days = 365;

            DateTime cutoff = DateTime.UtcNow.AddDays(-days);

            var old = db.AuditLogs.Where(l => l.CreatedAt < cutoff).ToList();
            db.AuditLogs.RemoveRange(old);
            db.SaveChanges();

            TempData["Success"] = old.Count + " audit entries older than " + days + " days removed.";
            return RedirectToAction("Index");
        }
    }
}