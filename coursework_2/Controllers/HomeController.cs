using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using coursework_2.Models;

namespace coursework_2.Controllers
{
    public class HomeController : BaseController
    {
        [AllowAnonymous]
        public ActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            if (!SettingsService.Current.LandingPageEnabled)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Title = "Welcome";
            ViewBag.Preview = false;
            return View("Landing", LoadSections(false));
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Preview()
        {
            ViewBag.Title = "Landing Page Preview";
            ViewBag.Preview = true;
            return View("Landing", LoadSections(true));
        }

        private List<LandingSection> LoadSections(bool includeUnpublished)
        {
            IQueryable<LandingSection> query = db.LandingSections.Include(s => s.Items);

            if (!includeUnpublished)
            {
                query = query.Where(s => s.IsPublished);
            }

            return query
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.SectionId)
                .ToList();
        }
    }
}
