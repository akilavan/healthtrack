using System;
using System.Linq;
using System.Web.Mvc;
using coursework_2.Models;

namespace coursework_2.Controllers
{
    public class ExercisesController : BaseController
    {
        public ActionResult Index(string q)
        {
            ViewBag.Title = "Exercise Library";
            ViewBag.Subtitle = "Fitness";
            ViewBag.Query = q;

            var list = db.Exercises.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                list = list.Where(e => e.Name.Contains(q) || e.Category.Contains(q));
            }

            return View(list.OrderBy(e => e.Name).ToList());
        }

        [Authorize(Roles = "Trainer,Admin")]
        [HttpGet]
        public ActionResult Form(int? id)
        {
            ViewBag.Title = id.HasValue ? "Edit Exercise" : "New Exercise";
            ViewBag.Subtitle = "Exercise Library";

            if (!id.HasValue) return View(new Exercise());

            var exercise = db.Exercises.Find(id.Value);
            if (exercise == null) return HttpNotFound();

            return View(exercise);
        }

        [Authorize(Roles = "Trainer,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Form(int? id, string name, string category, string muscleGroup, decimal metValue)
        {
            ViewBag.Title = id.HasValue ? "Edit Exercise" : "New Exercise";
            ViewBag.Subtitle = "Exercise Library";

            var model = new Exercise
            {
                ExerciseId = id ?? 0,
                Name = (name ?? "").Trim(),
                Category = (category ?? "").Trim(),
                MuscleGroup = (muscleGroup ?? "").Trim(),
                MetValue = metValue
            };

            if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Category))
            {
                ViewBag.Error = "Name and category are required.";
                return View(model);
            }

            if (model.MetValue < 0.5m || model.MetValue > 20m)
            {
                ViewBag.Error = "MET value must be between 0.5 and 20.";
                return View(model);
            }

            if (db.Exercises.Any(e => e.Name == model.Name && e.ExerciseId != model.ExerciseId))
            {
                ViewBag.Error = "An exercise with this name already exists.";
                return View(model);
            }

            bool isNew = !id.HasValue;

            if (isNew)
            {
                db.Exercises.Add(model);
            }
            else
            {
                var exercise = db.Exercises.Find(id.Value);
                if (exercise == null) return HttpNotFound();

                exercise.Name = model.Name;
                exercise.Category = model.Category;
                exercise.MuscleGroup = model.MuscleGroup;
                exercise.MetValue = model.MetValue;
            }

            db.SaveChanges();
            Audit(isNew ? "ExerciseCreated" : "ExerciseUpdated", "Exercises", id ?? model.ExerciseId);
            db.SaveChanges();

            TempData["Success"] = isNew ? "Exercise created." : "Exercise updated.";
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Trainer,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var exercise = db.Exercises.Find(id);
            if (exercise == null) return HttpNotFound();

            if (db.WorkoutPlanItems.Any(i => i.ExerciseId == id) || db.WorkoutLogs.Any(l => l.ExerciseId == id))
            {
                TempData["Error"] = "This exercise is used by a workout plan or log and cannot be deleted.";
                return RedirectToAction("Index");
            }

            db.Exercises.Remove(exercise);
            Audit("ExerciseDeleted", "Exercises", id);
            db.SaveChanges();

            TempData["Success"] = "Exercise deleted.";
            return RedirectToAction("Index");
        }
    }
}
