using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using coursework_2.Models;

namespace coursework_2.Controllers
{
    public class DietsController : BaseController
    {
        private static readonly string[] MealTypes = { "Breakfast", "Lunch", "Dinner", "Snack" };

        public ActionResult Index()
        {
            ViewBag.Title = "Diet Plans";
            ViewBag.Subtitle = "Nutrition";

            if (User.IsInRole("Client"))
            {
                var mine = db.DietPlans.Include("Trainer").Include("Meals")
                    .Where(d => d.ClientId == CurrentUserId)
                    .OrderByDescending(d => d.CreatedAt).ToList();
                return View(mine);
            }

            if (User.IsInRole("Trainer"))
            {
                var mine = db.DietPlans.Include("Client").Include("Meals")
                    .Where(d => d.TrainerId == CurrentUserId)
                    .OrderByDescending(d => d.CreatedAt).ToList();
                return View(mine);
            }

            var all = db.DietPlans.Include("Client").Include("Trainer").Include("Meals")
                .OrderByDescending(d => d.CreatedAt).Take(200).ToList();
            return View(all);
        }

        public ActionResult Details(int id = 0)
        {
            if (id == 0) return RedirectToAction("Index");
            var plan = db.DietPlans
                .Include("Client")
                .Include("Trainer")
                .Include("Meals.Food")
                .FirstOrDefault(d => d.DietPlanId == id);

            if (plan == null) return HttpNotFound();
            if (User.IsInRole("Client") && plan.ClientId != CurrentUserId) return new HttpStatusCodeResult(403);
            if (User.IsInRole("Trainer") && plan.TrainerId != CurrentUserId) return new HttpStatusCodeResult(403);

            ViewBag.Title = plan.Title;
            ViewBag.Subtitle = "Diet Plan";
            ViewBag.CanEdit = User.IsInRole("Trainer") && plan.TrainerId == CurrentUserId;
            ViewBag.Foods = db.Foods.OrderBy(f => f.Name).ToList();

            ViewBag.Logs = db.MealLogs.Include("Food")
                .Where(m => m.ClientId == plan.ClientId)
                .OrderByDescending(m => m.LogDate)
                .ThenByDescending(m => m.MealLogId)
                .Take(20).ToList();

            return View(plan);
        }

        [Authorize(Roles = "Trainer")]
        [HttpGet]
        public ActionResult Form(int? id)
        {
            ViewBag.Title = id.HasValue ? "Edit Diet Plan" : "New Diet Plan";
            ViewBag.Subtitle = "Diet Plans";

            PopulateClients();

            if (!id.HasValue)
                return View(new DietPlan { StartDate = DateTime.Today, Status = "Active", DailyCalorieTarget = 2000 });

            var plan = db.DietPlans.FirstOrDefault(d => d.DietPlanId == id.Value && d.TrainerId == CurrentUserId);
            if (plan == null) return new HttpStatusCodeResult(403);

            PopulateClients(plan.ClientId);
            return View(plan);
        }

        [Authorize(Roles = "Trainer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Form(int? id, int clientId, string title, int dailyCalorieTarget,
            DateTime startDate, DateTime? endDate, string status)
        {
            ViewBag.Title = id.HasValue ? "Edit Diet Plan" : "New Diet Plan";
            ViewBag.Subtitle = "Diet Plans";

            var model = new DietPlan
            {
                DietPlanId = id ?? 0,
                ClientId = clientId,
                Title = (title ?? "").Trim(),
                DailyCalorieTarget = dailyCalorieTarget,
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

            if (model.DailyCalorieTarget < 800 || model.DailyCalorieTarget > 6000)
            {
                ViewBag.Error = "Daily calorie target must be between 800 and 6000.";
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

            bool assigned = db.TrainerClients.Any(t =>
                t.TrainerId == CurrentUserId && t.ClientId == clientId && t.Status == "Active");

            if (!assigned)
            {
                ViewBag.Error = "You can only create plans for clients assigned to you.";
                PopulateClients(clientId);
                return View(model);
            }

            bool isNew = !id.HasValue;
            DietPlan plan;

            if (isNew)
            {
                plan = new DietPlan { TrainerId = CurrentUserId, CreatedAt = DateTime.UtcNow };
                db.DietPlans.Add(plan);
            }
            else
            {
                plan = db.DietPlans.FirstOrDefault(d => d.DietPlanId == id.Value && d.TrainerId == CurrentUserId);
                if (plan == null) return new HttpStatusCodeResult(403);
            }

            plan.ClientId = model.ClientId;
            plan.Title = model.Title;
            plan.DailyCalorieTarget = model.DailyCalorieTarget;
            plan.StartDate = model.StartDate;
            plan.EndDate = model.EndDate;
            plan.Status = model.Status;

            db.SaveChanges();
            Audit(isNew ? "DietPlanCreated" : "DietPlanUpdated", "DietPlans", plan.DietPlanId);
            db.SaveChanges();

            TempData["Success"] = isNew ? "Plan created. Add meals below." : "Plan updated.";
            return RedirectToAction("Details", new { id = plan.DietPlanId });
        }

        [Authorize(Roles = "Trainer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddMeal(int planId, string mealType, byte dayOfWeek, string foodName,
            decimal calories, decimal quantity, TimeSpan? mealTime)
        {
            var plan = db.DietPlans.FirstOrDefault(d => d.DietPlanId == planId && d.TrainerId == CurrentUserId);
            if (plan == null) return new HttpStatusCodeResult(403);

            if (!MealTypes.Contains(mealType) || dayOfWeek > 6 ||
                string.IsNullOrWhiteSpace(foodName) || calories <= 0)
            {
                TempData["Error"] = "Choose a valid meal type, day and food.";
                return RedirectToAction("Details", new { id = planId });
            }

            foodName = foodName.Trim();
            if (quantity <= 0) quantity = 1;

            var food = db.Foods.FirstOrDefault(f => f.Name == foodName);
            decimal unitCalories = food != null ? food.Calories : calories;
            decimal mealCalories = unitCalories * quantity;

            var dayMeals = db.DietPlanMeals.Include("Food")
                .Where(m => m.DietPlanId == planId && m.DayOfWeek == dayOfWeek)
                .ToList();

            decimal dayTotal = dayMeals.Sum(m => m.Food != null ? m.Food.Calories * m.Quantity : 0m);
            decimal projected = dayTotal + mealCalories;

            if (projected > plan.DailyCalorieTarget)
            {
                TempData["Error"] = string.Format(
                    "Cannot add this meal: {0} would reach {1:0.#} kcal, which is {2:0.#} kcal over the {3} kcal daily target.",
                    (DayOfWeek)dayOfWeek, projected, projected - plan.DailyCalorieTarget, plan.DailyCalorieTarget);

                return RedirectToAction("Details", new { id = planId });
            }

            if (food == null)
            {
                food = new Food
                {
                    Name = foodName,
                    ServingDesc = "1 serving",
                    Calories = calories,
                    ProteinG = 0,
                    CarbsG = 0,
                    FatG = 0
                };
                db.Foods.Add(food);
                db.SaveChanges();
            }

            db.DietPlanMeals.Add(new DietPlanMeal
            {
                DietPlanId = planId,
                FoodId = food.FoodId,
                MealType = mealType,
                DayOfWeek = dayOfWeek,
                Quantity = quantity,
                MealTime = mealTime
            });

            Audit("DietMealAdded", "DietPlanMeals", planId);
            db.SaveChanges();

            if (projected < plan.DailyCalorieTarget)
            {
                TempData["Warning"] = string.Format(
                    "Meal added, but {0} is now {1:0.#} kcal — {2:0.#} kcal below the {3} kcal daily target.",
                    (DayOfWeek)dayOfWeek, projected, plan.DailyCalorieTarget - projected, plan.DailyCalorieTarget);
            }
            else
            {
                TempData["Success"] = "Meal added. " + (DayOfWeek)dayOfWeek + " now meets the daily target.";
            }

            return RedirectToAction("Details", new { id = planId });
        }

        [Authorize(Roles = "Trainer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveMeal(int id)
        {
            var meal = db.DietPlanMeals.Include("DietPlan").FirstOrDefault(m => m.DietPlanMealId == id);
            if (meal == null) return HttpNotFound();
            if (meal.DietPlan.TrainerId != CurrentUserId) return new HttpStatusCodeResult(403);

            int planId = meal.DietPlanId;

            db.DietPlanMeals.Remove(meal);
            Audit("DietMealRemoved", "DietPlanMeals", id);
            db.SaveChanges();

            TempData["Success"] = "Meal removed.";
            return RedirectToAction("Details", new { id = planId });
        }

        [Authorize(Roles = "Client")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogMeal(int id)
        {
            var meal = db.DietPlanMeals.Include("Food").Include("DietPlan")
                .FirstOrDefault(m => m.DietPlanMealId == id);

            if (meal == null) return HttpNotFound();
            if (meal.DietPlan.ClientId != CurrentUserId) return new HttpStatusCodeResult(403);

            DateTime today = DateTime.Today;

            if (db.MealLogs.Any(l => l.ClientId == CurrentUserId && l.FoodId == meal.FoodId
                                  && l.MealType == meal.MealType && l.LogDate == today))
            {
                TempData["Error"] = "This meal is already logged today.";
                return RedirectToAction("Details", new { id = meal.DietPlanId });
            }

            db.MealLogs.Add(new MealLog
            {
                ClientId = CurrentUserId,
                FoodId = meal.FoodId,
                MealType = meal.MealType,
                LogDate = today,
                Quantity = meal.Quantity,
                Calories = meal.Food.Calories * meal.Quantity,
                CreatedAt = DateTime.UtcNow
            });

            Audit("MealLogged", "MealLogs", meal.DietPlanId);
            db.SaveChanges();

            TempData["Success"] = "Meal logged.";
            return RedirectToAction("Details", new { id = meal.DietPlanId });
        }

        [Authorize(Roles = "Trainer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var plan = db.DietPlans.FirstOrDefault(d => d.DietPlanId == id && d.TrainerId == CurrentUserId);
            if (plan == null) return new HttpStatusCodeResult(403);

            db.DietPlans.Remove(plan);
            Audit("DietPlanDeleted", "DietPlans", id);
            db.SaveChanges();

            TempData["Success"] = "Diet plan deleted.";
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