using System.Linq;
using System.Web.Mvc;
using coursework_2.Models;

namespace coursework_2.Controllers
{
    public class NotificationsController : BaseController
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Notifications";
            ViewBag.Subtitle = "Account";

            var list = db.Notifications
                .Where(n => n.UserId == CurrentUserId)
                .OrderByDescending(n => n.ScheduledFor)
                .Take(100).ToList();

            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkRead(int id)
        {
            var n = db.Notifications.FirstOrDefault(x => x.NotificationId == id && x.UserId == CurrentUserId);
            if (n == null) return HttpNotFound();

            n.IsRead = true;
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkAllRead()
        {
            foreach (var n in db.Notifications.Where(x => x.UserId == CurrentUserId && !x.IsRead).ToList())
                n.IsRead = true;

            db.SaveChanges();

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var n = db.Notifications.FirstOrDefault(x => x.NotificationId == id && x.UserId == CurrentUserId);
            if (n == null) return HttpNotFound();

            db.Notifications.Remove(n);
            db.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}