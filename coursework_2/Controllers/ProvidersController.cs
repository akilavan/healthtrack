using coursework_2.Models;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.WebPages;
using ValidationHelper = coursework_2.Models.ValidationHelper;

namespace coursework_2.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProvidersController : BaseController
    {
        public ActionResult Index(string q)
        {
            ViewBag.Title = "Healthcare Providers";
            ViewBag.Subtitle = "Administration";
            ViewBag.Query = q;

            var list = db.HealthcareProviders.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                list = list.Where(p => p.FullName.Contains(q) || p.Specialty.Contains(q) || p.Clinic.Contains(q));
            }

            return View(list.OrderBy(p => p.FullName).ToList());
        }

        [HttpGet]
        public ActionResult Form(int? id)
        {
            ViewBag.Title = id.HasValue ? "Edit Provider" : "New Provider";
            ViewBag.Subtitle = "Healthcare Providers";

            if (!id.HasValue)
                return View(new HealthcareProvider());

            var provider = db.HealthcareProviders.Find(id.Value);
            if (provider == null) return HttpNotFound();

            return View(provider);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Form(int? id, string fullName, string specialty, string clinic, string phone, bool isActive)
        {
            ViewBag.Title = id.HasValue ? "Edit Provider" : "New Provider";
            ViewBag.Subtitle = "Healthcare Providers";

            fullName = (fullName ?? "").Trim();
            specialty = (specialty ?? "").Trim();
            clinic = (clinic ?? "").Trim();
            phone = (phone ?? "").Trim();

            var model = new HealthcareProvider
            {
                ProviderId = id ?? 0,
                FullName = fullName,
                Specialty = specialty,
                Clinic = string.IsNullOrWhiteSpace(clinic) ? null : clinic,
                Phone = ValidationHelper.NormalizePhone(phone),
                IsActive = isActive
            };

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(specialty))
            {
                ViewBag.Error = "Full name and specialty are required.";
                return View(model);
            }
            if (!string.IsNullOrWhiteSpace(phone) && !ValidationHelper.IsValidPhone(phone))
            {
                ViewBag.Error = "Enter a valid phone number (e.g. 0112345678 or +94112345678).";
                return View(model);
            }

            bool isNew = !id.HasValue;
            HealthcareProvider provider;

            if (isNew)
            {
                provider = new HealthcareProvider();
                db.HealthcareProviders.Add(provider);
            }
            else
            {
                provider = db.HealthcareProviders.Find(id.Value);
                if (provider == null) return HttpNotFound();
            }

            provider.FullName = model.FullName;
            provider.Specialty = model.Specialty;
            provider.Clinic = model.Clinic;
            provider.Phone = model.Phone;
            provider.IsActive = model.IsActive;

            db.SaveChanges();
            Audit(isNew ? "ProviderCreated" : "ProviderUpdated", "HealthcareProviders", provider.ProviderId);
            db.SaveChanges();

            TempData["Success"] = isNew ? "Provider created." : "Provider updated.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var provider = db.HealthcareProviders.Find(id);
            if (provider == null) return HttpNotFound();

            if (db.Appointments.Any(a => a.ProviderId == id))
            {
                provider.IsActive = false;
                db.SaveChanges();
                TempData["Error"] = "This provider has appointments and was deactivated instead of deleted.";
                return RedirectToAction("Index");
            }

            db.HealthcareProviders.Remove(provider);
            Audit("ProviderDeleted", "HealthcareProviders", id);
            db.SaveChanges();

            TempData["Success"] = "Provider deleted.";
            return RedirectToAction("Index");
        }
    }
}