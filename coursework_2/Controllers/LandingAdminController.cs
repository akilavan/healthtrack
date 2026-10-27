using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using coursework_2.Models;

namespace coursework_2.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LandingAdminController : BaseController
    {
        private static readonly string[] AllowedImageExtensions = { ".png", ".jpg", ".jpeg", ".gif", ".webp" };
        private const int MaxImageBytes = 3 * 1024 * 1024;
        private const string UploadFolder = "~/assets/uploads/landing/";

        public ActionResult Index()
        {
            ViewBag.Title = "Landing Page";
            ViewBag.Subtitle = "Content";

            var sections = db.LandingSections
                .Include("Items")
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.SectionId)
                .ToList();

            return View(sections);
        }

        [HttpGet]
        public ActionResult Form(int? id)
        {
            ViewBag.Title = id.HasValue ? "Edit Landing Section" : "New Landing Section";
            ViewBag.Subtitle = "Landing Page";
            ViewBag.SectionTypes = LandingSection.Types;
            ViewBag.BackgroundStyles = LandingSection.BackgroundStyles;

            if (!id.HasValue)
            {
                return View(new LandingSection
                {
                    SectionType = "Hero",
                    BackgroundStyle = "light",
                    IsPublished = true,
                    SortOrder = NextSectionOrder()
                });
            }

            var section = db.LandingSections.Find(id.Value);
            if (section == null) return HttpNotFound();
            return View(section);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Form(int? id, string sectionType, string heading, string subheading,
            string body, string backgroundStyle, string buttonText, string buttonUrl,
            int sortOrder, bool isPublished, HttpPostedFileBase image, bool removeImage = false)
        {
            ViewBag.Title = id.HasValue ? "Edit Landing Section" : "New Landing Section";
            ViewBag.Subtitle = "Landing Page";
            ViewBag.SectionTypes = LandingSection.Types;
            ViewBag.BackgroundStyles = LandingSection.BackgroundStyles;

            sectionType = (sectionType ?? "").Trim();
            heading = (heading ?? "").Trim();
            subheading = (subheading ?? "").Trim();
            body = (body ?? "").Trim();
            backgroundStyle = (backgroundStyle ?? "").Trim().ToLowerInvariant();
            buttonText = (buttonText ?? "").Trim();
            buttonUrl = (buttonUrl ?? "").Trim();

            LandingSection section;
            if (id.HasValue)
            {
                section = db.LandingSections.Find(id.Value);
                if (section == null) return HttpNotFound();
            }
            else
            {
                section = new LandingSection { CreatedAt = DateTime.UtcNow };
            }

            section.SectionType = sectionType;
            section.Heading = heading;
            section.Subheading = subheading;
            section.Body = body;
            section.BackgroundStyle = backgroundStyle;
            section.ButtonText = buttonText;
            section.ButtonUrl = buttonUrl;
            section.SortOrder = sortOrder;
            section.IsPublished = isPublished;

            if (!LandingSection.Types.Contains(sectionType))
            {
                ViewBag.Error = "Select a valid section type.";
                return View(section);
            }

            if (string.IsNullOrWhiteSpace(heading))
            {
                ViewBag.Error = "Heading is required.";
                return View(section);
            }

            if (heading.Length > 160 || subheading.Length > 300 || body.Length > 2000
                || buttonText.Length > 80 || buttonUrl.Length > 300)
            {
                ViewBag.Error = "One or more fields exceed the maximum length.";
                return View(section);
            }

            if (!LandingSection.BackgroundStyles.Contains(backgroundStyle))
            {
                ViewBag.Error = "Select a valid background style.";
                return View(section);
            }

            if (!IsSafeUrl(buttonUrl))
            {
                ViewBag.Error = "Button URL must be a relative path, an anchor, or an http/https link.";
                return View(section);
            }

            string imageError = ValidateImage(image);
            if (imageError != null)
            {
                ViewBag.Error = imageError;
                return View(section);
            }

            string oldImage = section.ImagePath;
            if (image != null && image.ContentLength > 0)
            {
                section.ImagePath = SaveImage(image, "section");
            }
            else if (removeImage)
            {
                section.ImagePath = null;
            }

            if (section.SortOrder <= 0)
            {
                section.SortOrder = id.HasValue && section.SortOrder == 0
                    ? section.SortOrder
                    : NextSectionOrder();
            }

            section.UpdatedAt = DateTime.UtcNow;

            if (!id.HasValue)
            {
                db.LandingSections.Add(section);
            }

            db.SaveChanges();

            if (!string.Equals(oldImage, section.ImagePath, StringComparison.OrdinalIgnoreCase))
            {
                DeleteImage(oldImage);
            }

            Audit(id.HasValue ? "LandingSectionUpdated" : "LandingSectionCreated", "LandingSections", section.SectionId);
            db.SaveChanges();

            TempData["Success"] = id.HasValue ? "Section updated." : "Section created.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TogglePublish(int id)
        {
            var section = db.LandingSections.Find(id);
            if (section == null) return HttpNotFound();

            section.IsPublished = !section.IsPublished;
            section.UpdatedAt = DateTime.UtcNow;
            db.SaveChanges();

            Audit("LandingSectionPublished", "LandingSections", section.SectionId);
            db.SaveChanges();

            TempData["Success"] = section.IsPublished ? "Section published." : "Section hidden from the public page.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Move(int id, string direction)
        {
            var sections = db.LandingSections
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.SectionId)
                .ToList();

            int index = sections.FindIndex(s => s.SectionId == id);
            if (index < 0) return HttpNotFound();

            int target = direction == "up" ? index - 1 : index + 1;
            if (target >= 0 && target < sections.Count)
            {
                var temp = sections[index];
                sections[index] = sections[target];
                sections[target] = temp;

                for (int i = 0; i < sections.Count; i++)
                {
                    sections[i].SortOrder = i + 1;
                }
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var section = db.LandingSections.Include("Items").FirstOrDefault(s => s.SectionId == id);
            if (section == null) return HttpNotFound();

            var images = new List<string>();
            if (!string.IsNullOrWhiteSpace(section.ImagePath)) images.Add(section.ImagePath);
            foreach (var item in section.Items)
            {
                if (!string.IsNullOrWhiteSpace(item.ImagePath)) images.Add(item.ImagePath);
            }

            db.LandingSections.Remove(section);
            db.SaveChanges();

            foreach (var imagePath in images)
            {
                DeleteImage(imagePath);
            }

            Audit("LandingSectionDeleted", "LandingSections", id);
            db.SaveChanges();

            TempData["Success"] = "Section deleted.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateDefaults()
        {
            if (db.LandingSections.Any())
            {
                TempData["Error"] = "Starter content can only be added when there are no sections.";
                return RedirectToAction("Index");
            }

            int order = 1;

            var hero = new LandingSection
            {
                SectionType = "Hero",
                Heading = "A healthier routine starts here",
                Subheading = "HealthTrack brings your workouts, nutrition, progress and coaching together in one clear, motivating space.",
                Body = "Built for clients, trainers and administrators who want less admin and more progress.",
                BackgroundStyle = "primary",
                ButtonText = "Create your account",
                ButtonUrl = "/Account/Register",
                SortOrder = order++,
                IsPublished = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.LandingSections.Add(hero);

            var features = new LandingSection
            {
                SectionType = "Features",
                Heading = "Everything you need in one place",
                Subheading = "Simple tools that keep your plan, your progress and your support network connected.",
                BackgroundStyle = "light",
                SortOrder = order++,
                IsPublished = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            features.Items.Add(new LandingSectionItem { Title = "Personalised workout plans", Text = "Your trainer builds weekly routines around your goals, schedule and experience.", SortOrder = 1 });
            features.Items.Add(new LandingSectionItem { Title = "Nutrition that fits your day", Text = "Follow a calorie-aware meal plan and log what you eat without the guesswork.", SortOrder = 2 });
            features.Items.Add(new LandingSectionItem { Title = "Progress you can feel", Text = "Track weight, BMI, body measurements and sessions in one simple dashboard.", SortOrder = 3 });
            db.LandingSections.Add(features);

            var about = new LandingSection
            {
                SectionType = "About",
                Heading = "Coaching, without the clutter",
                Subheading = "One shared workspace for every step of the journey.",
                Body = "HealthTrack gives clients a clear view of what to do next, trainers the tools to support them, and administrators the oversight to keep everything running smoothly.",
                BackgroundStyle = "muted",
                ButtonText = "Explore the features",
                ButtonUrl = "#features",
                SortOrder = order++,
                IsPublished = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.LandingSections.Add(about);

            var stats = new LandingSection
            {
                SectionType = "Stats",
                Heading = "A healthier routine, made simple",
                Subheading = "Small, consistent actions add up to meaningful change.",
                BackgroundStyle = "light",
                SortOrder = order++,
                IsPublished = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            stats.Items.Add(new LandingSectionItem { Title = "3", Text = "connected roles", SortOrder = 1 });
            stats.Items.Add(new LandingSectionItem { Title = "7 days", Text = "of visible planning", SortOrder = 2 });
            stats.Items.Add(new LandingSectionItem { Title = "1 place", Text = "for every milestone", SortOrder = 3 });
            db.LandingSections.Add(stats);

            var testimonials = new LandingSection
            {
                SectionType = "Testimonials",
                Heading = "Built around real routines",
                Subheading = "A few words from people using HealthTrack.",
                BackgroundStyle = "muted",
                SortOrder = order++,
                IsPublished = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            testimonials.Items.Add(new LandingSectionItem { Title = "Sathana S.", Text = "Having my workouts and meals in one place makes it much easier to stay consistent.", SortOrder = 1 });
            testimonials.Items.Add(new LandingSectionItem { Title = "Nimal P.", Text = "I can see how my clients are progressing between sessions and adjust their plans quickly.", SortOrder = 2 });
            testimonials.Items.Add(new LandingSectionItem { Title = "HealthTrack member", Text = "The dashboard turns a busy week into a simple list of next steps.", SortOrder = 3 });
            db.LandingSections.Add(testimonials);

            var faq = new LandingSection
            {
                SectionType = "FAQ",
                Heading = "Frequently asked questions",
                Subheading = "Everything you need to know before getting started.",
                BackgroundStyle = "light",
                SortOrder = order++,
                IsPublished = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            faq.Items.Add(new LandingSectionItem { Title = "Do I need a trainer?", Text = "No. You can register as a client and use the tracking tools independently, or ask an administrator to assign a trainer.", SortOrder = 1 });
            faq.Items.Add(new LandingSectionItem { Title = "Can I track my meals?", Text = "Yes. Your diet plan includes a daily calorie target and lets you log each meal against it.", SortOrder = 2 });
            faq.Items.Add(new LandingSectionItem { Title = "Is my progress saved?", Text = "Yes. Your measurements, workout logs and meal logs are stored in your account so you can review them over time.", SortOrder = 3 });
            db.LandingSections.Add(faq);

            var cta = new LandingSection
            {
                SectionType = "CTA",
                Heading = "Ready to take the next step?",
                Body = "Create your free account and start building a routine that lasts.",
                BackgroundStyle = "primary",
                ButtonText = "Get started",
                ButtonUrl = "/Account/Register",
                SortOrder = order,
                IsPublished = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.LandingSections.Add(cta);

            db.SaveChanges();

            Audit("LandingDefaultsCreated", "LandingSections");
            db.SaveChanges();

            TempData["Success"] = "Starter landing content added.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Items(int id)
        {
            var section = db.LandingSections.Include("Items").FirstOrDefault(s => s.SectionId == id);
            if (section == null) return HttpNotFound();

            ViewBag.Title = "Manage Items";
            ViewBag.Subtitle = "Landing Page";
            return View(section);
        }

        [HttpGet]
        public ActionResult ItemForm(int sectionId, int? id)
        {
            var section = db.LandingSections.Find(sectionId);
            if (section == null) return HttpNotFound();

            ViewBag.Section = section;
            ViewBag.Title = id.HasValue ? "Edit Item" : "New Item";
            ViewBag.Subtitle = "Landing Page";

            if (!id.HasValue)
            {
                return View(new LandingSectionItem
                {
                    SectionId = sectionId,
                    SortOrder = NextItemOrder(sectionId)
                });
            }

            var item = db.LandingSectionItems.FirstOrDefault(i => i.ItemId == id.Value && i.SectionId == sectionId);
            if (item == null) return HttpNotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ItemForm(int sectionId, int? id, string title, string text,
            string linkText, string linkUrl, int sortOrder, HttpPostedFileBase image, bool removeImage = false)
        {
            var section = db.LandingSections.Find(sectionId);
            if (section == null) return HttpNotFound();

            ViewBag.Section = section;
            ViewBag.Title = id.HasValue ? "Edit Item" : "New Item";
            ViewBag.Subtitle = "Landing Page";

            title = (title ?? "").Trim();
            text = (text ?? "").Trim();
            linkText = (linkText ?? "").Trim();
            linkUrl = (linkUrl ?? "").Trim();

            LandingSectionItem item;
            if (id.HasValue)
            {
                item = db.LandingSectionItems.FirstOrDefault(i => i.ItemId == id.Value && i.SectionId == sectionId);
                if (item == null) return HttpNotFound();
            }
            else
            {
                item = new LandingSectionItem { SectionId = sectionId };
            }

            item.Title = title;
            item.Text = text;
            item.LinkText = linkText;
            item.LinkUrl = linkUrl;
            item.SortOrder = sortOrder;

            if (string.IsNullOrWhiteSpace(title))
            {
                ViewBag.Error = "Title is required.";
                return View(item);
            }

            if (title.Length > 160 || text.Length > 1200 || linkText.Length > 80 || linkUrl.Length > 300)
            {
                ViewBag.Error = "One or more fields exceed the maximum length.";
                return View(item);
            }

            if (!IsSafeUrl(linkUrl))
            {
                ViewBag.Error = "Link URL must be a relative path, an anchor, or an http/https link.";
                return View(item);
            }

            string imageError = ValidateImage(image);
            if (imageError != null)
            {
                ViewBag.Error = imageError;
                return View(item);
            }

            string oldImage = item.ImagePath;
            if (image != null && image.ContentLength > 0)
            {
                item.ImagePath = SaveImage(image, "item");
            }
            else if (removeImage)
            {
                item.ImagePath = null;
            }

            if (item.SortOrder <= 0)
            {
                item.SortOrder = NextItemOrder(sectionId);
            }

            if (!id.HasValue)
            {
                db.LandingSectionItems.Add(item);
            }

            db.SaveChanges();

            if (!string.Equals(oldImage, item.ImagePath, StringComparison.OrdinalIgnoreCase))
            {
                DeleteImage(oldImage);
            }

            Audit(id.HasValue ? "LandingItemUpdated" : "LandingItemCreated", "LandingSectionItems", item.ItemId);
            db.SaveChanges();

            TempData["Success"] = id.HasValue ? "Item updated." : "Item created.";
            return RedirectToAction("Items", new { id = sectionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ItemDelete(int id)
        {
            var item = db.LandingSectionItems.Find(id);
            if (item == null) return HttpNotFound();

            int sectionId = item.SectionId;
            string imagePath = item.ImagePath;

            db.LandingSectionItems.Remove(item);
            db.SaveChanges();
            DeleteImage(imagePath);

            Audit("LandingItemDeleted", "LandingSectionItems", id);
            db.SaveChanges();

            TempData["Success"] = "Item deleted.";
            return RedirectToAction("Items", new { id = sectionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ItemMove(int id, string direction)
        {
            var item = db.LandingSectionItems.Find(id);
            if (item == null) return HttpNotFound();

            var items = db.LandingSectionItems
                .Where(i => i.SectionId == item.SectionId)
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.ItemId)
                .ToList();

            int index = items.FindIndex(i => i.ItemId == id);
            int target = direction == "up" ? index - 1 : index + 1;

            if (target >= 0 && target < items.Count)
            {
                var temp = items[index];
                items[index] = items[target];
                items[target] = temp;

                for (int i = 0; i < items.Count; i++)
                {
                    items[i].SortOrder = i + 1;
                }
                db.SaveChanges();
            }

            return RedirectToAction("Items", new { id = item.SectionId });
        }

        private int NextSectionOrder()
        {
            int? max = db.LandingSections.Select(s => (int?)s.SortOrder).Max();
            return (max ?? 0) + 1;
        }

        private int NextItemOrder(int sectionId)
        {
            int? max = db.LandingSectionItems
                .Where(i => i.SectionId == sectionId)
                .Select(i => (int?)i.SortOrder)
                .Max();
            return (max ?? 0) + 1;
        }

        private static bool IsSafeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return true;
            url = url.Trim();

            if (url.StartsWith("#")) return true;
            if (url.StartsWith("/") && !url.StartsWith("//")) return true;

            Uri parsed;
            return Uri.TryCreate(url, UriKind.Absolute, out parsed)
                && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps);
        }

        private static string ValidateImage(HttpPostedFileBase image)
        {
            if (image == null || image.ContentLength == 0) return null;

            string extension = Path.GetExtension(image.FileName ?? "").ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(extension))
            {
                return "Image must be a PNG, JPG, GIF or WEBP file.";
            }

            if (image.ContentLength > MaxImageBytes)
            {
                return "Image must be 3 MB or smaller.";
            }

            return null;
        }

        private string SaveImage(HttpPostedFileBase image, string prefix)
        {
            string extension = Path.GetExtension(image.FileName ?? "").ToLowerInvariant();
            string folder = Server.MapPath(UploadFolder);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string fileName = prefix + "-" + Guid.NewGuid().ToString("N") + extension;
            image.SaveAs(Path.Combine(folder, fileName));
            return UploadFolder + fileName;
        }

        private void DeleteImage(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !path.StartsWith(UploadFolder)) return;

            try
            {
                string physical = Server.MapPath(path);
                if (System.IO.File.Exists(physical)) System.IO.File.Delete(physical);
            }
            catch
            {
            }
        }
    }
}
