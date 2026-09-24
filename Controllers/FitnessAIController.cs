using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace DUT_Campus_FIT_Gym.Controllers
{
    [Authorize(Roles = "Student,Staff")]
    public class FitnessAIController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GymDbContext _context;

        public FitnessAIController(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            GymDbContext context)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult AIWorkout()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateWorkout(
            string fitnessLevel,
            string fitnessGoal,
            string preferredExercises,
            int daysAvailable)
        {
            if (string.IsNullOrWhiteSpace(fitnessLevel) ||
                string.IsNullOrWhiteSpace(fitnessGoal) ||
                daysAvailable < 2 ||
                daysAvailable > 5)
            {
                ViewBag.Error =
                    "Please complete all required workout options.";

                return View("AIWorkout");
            }

            var validLevels = new[]
            {
                "Beginner",
                "Intermediate",
                "Advanced"
            };

            var validGoals = new[]
            {
                "General fitness",
                "Strength",
                "Endurance",
                "Mobility and flexibility"
            };

            if (!validLevels.Contains(fitnessLevel) ||
                !validGoals.Contains(fitnessGoal))
            {
                ViewBag.Error =
                    "Please select valid workout preferences.";

                return View("AIWorkout");
            }

            string? apiKey =
                _configuration["GeminiApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                ViewBag.Error =
                    "The Fitness AI service is not configured. GeminiApiKey was not found.";

                return View("AIWorkout");
            }

            apiKey = apiKey.Trim();

            string exercisePreference =
                string.IsNullOrWhiteSpace(preferredExercises)
                    ? "No specific exercises were requested."
                    : preferredExercises.Trim();

            string prompt =
                "You are the Fitness AI assistant for DUT Campus FIT Gym.\n\n" +
                $"Create a safe and sensible {daysAvailable}-day weekly workout plan.\n\n" +
                $"Fitness level: {fitnessLevel}\n" +
                $"Fitness goal: {fitnessGoal}\n" +
                $"Preferred exercises: {exercisePreference}\n\n" +
                $"Generate exercises across exactly {daysAvailable} distinct workout days.\n" +
                "Use recovery days where appropriate.\n\n" +
                "Rules:\n" +
                "- Keep exercise volume reasonable.\n" +
                "- Do not recommend extreme exercise amounts.\n" +
                "- Do not create dangerous challenges.\n" +
                "- Use beginner-appropriate exercises for Beginner level.\n" +
                "- Respect preferred exercises where appropriate.\n" +
                "- Include recovery where appropriate.\n" +
                "- Sets must be between 1 and 5.\n" +
                "- Repetitions must be between 1 and 20.\n" +
                "- RestTime must be between 30 and 180 seconds.\n" +
                "- Do not include medical diagnoses.\n" +
                "- Do not prescribe exercise as treatment for injuries or medical conditions.\n" +
                "- Do not include warm-ups or cool-downs as exercises.\n" +
                "- Keep descriptions short and practical.\n" +
                "- Return only the workout exercises.";

            var responseSchema = new
            {
                type = "array",
                minItems = daysAvailable,
                maxItems = daysAvailable * 4,
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        workoutDay = new
                        {
                            type = "string"
                        },
                        exerciseName = new
                        {
                            type = "string"
                        },
                        sets = new
                        {
                            type = "integer",
                            minimum = 1,
                            maximum = 5
                        },
                        repetitions = new
                        {
                            type = "integer",
                            minimum = 1,
                            maximum = 20
                        },
                        restTime = new
                        {
                            type = "integer",
                            minimum = 30,
                            maximum = 180
                        },
                        description = new
                        {
                            type = "string"
                        }
                    },
                    required = new[]
                    {
                        "workoutDay",
                        "exerciseName",
                        "sets",
                        "repetitions",
                        "restTime",
                        "description"
                    }
                }
            };

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = prompt
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    responseSchema = responseSchema
                }
            };

            string json =
                JsonSerializer.Serialize(requestBody);

            var client =
                _httpClientFactory.CreateClient();

            string url =
                "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash-lite:generateContent";

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    url);

            request.Headers.Add(
                "x-goog-api-key",
                apiKey);

            request.Content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

            try
            {
                var response =
                    await client.SendAsync(request);

                string responseContent =
                    await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error =
                        $"Gemini error {(int)response.StatusCode}: {responseContent}";

                    return View("AIWorkout");
                }

                using JsonDocument document =
                    JsonDocument.Parse(responseContent);

                if (!document.RootElement.TryGetProperty(
                        "candidates",
                        out JsonElement candidates) ||
                    candidates.GetArrayLength() == 0)
                {
                    ViewBag.Error =
                        "Gemini did not return any candidates.";

                    return View("AIWorkout");
                }

                var candidate =
                    candidates[0];

                if (!candidate.TryGetProperty(
                        "content",
                        out JsonElement content) ||
                    !content.TryGetProperty(
                        "parts",
                        out JsonElement parts))
                {
                    ViewBag.Error =
                        "Gemini returned a response without usable content.";

                    return View("AIWorkout");
                }

                string aiText = "";

                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty(
                            "text",
                            out JsonElement text))
                    {
                        aiText =
                            text.GetString() ?? "";

                        break;
                    }
                }

                if (string.IsNullOrWhiteSpace(aiText))
                {
                    ViewBag.Error =
                        "Gemini returned an empty workout plan.";

                    return View("AIWorkout");
                }

                aiText = aiText
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                var exercises =
                    JsonSerializer.Deserialize<
                        List<AIWorkoutExerciseViewModel>>(
                            aiText,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                if (exercises == null ||
                    exercises.Count == 0)
                {
                    ViewBag.Error =
                        "Gemini returned an empty or invalid workout plan.";

                    return View("AIWorkout");
                }

                exercises =
                    exercises
                        .Where(e =>
                            !string.IsNullOrWhiteSpace(
                                e.WorkoutDay) &&
                            !string.IsNullOrWhiteSpace(
                                e.ExerciseName) &&
                            e.Sets >= 1 &&
                            e.Sets <= 5 &&
                            e.Repetitions >= 1 &&
                            e.Repetitions <= 20 &&
                            e.RestTime >= 30 &&
                            e.RestTime <= 180)
                        .ToList();

                if (exercises.Count == 0)
                {
                    ViewBag.Error =
                        "Gemini generated exercises outside the allowed limits.";

                    return View("AIWorkout");
                }

                var workoutDays =
                    exercises
                        .Select(e =>
                            e.WorkoutDay.Trim())
                        .Distinct(
                            StringComparer.OrdinalIgnoreCase)
                        .Count();

                if (workoutDays != daysAvailable)
                {
                    ViewBag.Error =
                        $"Gemini generated {workoutDays} workout days instead of the requested {daysAvailable}. Please try again.";

                    return View("AIWorkout");
                }

                if (exercises.Count >
                    daysAvailable * 4)
                {
                    ViewBag.Error =
                        "Gemini generated too many exercises. Please try again.";

                    return View("AIWorkout");
                }

                ViewBag.FitnessLevel =
                    fitnessLevel;

                ViewBag.FitnessGoal =
                    fitnessGoal;

                ViewBag.DaysAvailable =
                    daysAvailable;

                return View(
                    "AIWorkoutResult",
                    exercises);
            }
            catch (JsonException ex)
            {
                ViewBag.Error =
                    $"JSON processing error: {ex.Message}";

                return View("AIWorkout");
            }
            catch (HttpRequestException ex)
            {
                ViewBag.Error =
                    $"HTTP request error: {ex.Message}";

                return View("AIWorkout");
            }
            catch (TaskCanceledException ex)
            {
                ViewBag.Error =
                    $"The Gemini request timed out: {ex.Message}";

                return View("AIWorkout");
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    $"Unexpected error: {ex.Message}";

                return View("AIWorkout");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveWorkout(
            List<AIWorkoutExerciseViewModel> exercises,
            string fitnessLevel)
        {
            var memberId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberId))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            if (exercises == null ||
                !exercises.Any())
            {
                return RedirectToAction(
                    "AIWorkout");
            }

            int currentMemberId =
                int.Parse(memberId);

            if (string.IsNullOrWhiteSpace(fitnessLevel))
            {
                fitnessLevel = "Beginner";
            }

            fitnessLevel =
                fitnessLevel.Trim();

            if (fitnessLevel.Equals(
                    "Advanced",
                    StringComparison.OrdinalIgnoreCase))
            {
                fitnessLevel = "Pro";
            }

            var validLevels = new[]
            {
                "Beginner",
                "Intermediate",
                "Pro"
            };

            if (!validLevels.Contains(
                    fitnessLevel,
                    StringComparer.OrdinalIgnoreCase))
            {
                fitnessLevel = "Beginner";
            }

            var workoutName =
                $"{fitnessLevel} AI Workout";

            var exerciseLibrary =
                _context.Exercises.ToList();

            foreach (var exercise in exercises)
            {
                var libraryExercise =
                    exerciseLibrary.FirstOrDefault(e =>
                        e.ExerciseName.Equals(
                            exercise.ExerciseName.Trim(),
                            StringComparison.OrdinalIgnoreCase));

                if (libraryExercise == null)
                {
                    continue;
                }

                _context.WorkoutPlans.Add(
                    new WorkoutPlan
                    {
                        MemberId =
                            currentMemberId,

                        WorkoutName =
                            workoutName,

                        Level =
                            fitnessLevel,

                        ExerciseId =
                            libraryExercise.ExerciseId,

                        WorkoutDay =
                            exercise.WorkoutDay,

                        Sets =
                            exercise.Sets,

                        Repetitions =
                            exercise.Repetitions,

                        RestTime =
                            exercise.RestTime,

                        Description =
                            exercise.Description
                    });
            }

            _context.SaveChanges();

            return RedirectToAction(
                "MyWorkout",
                "Workout");
        }

        [HttpGet]
        public IActionResult AIMeal()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateMealPlan(
            string fitnessGoal,
            string foodPreferences,
            string dietaryPreference)
        {
            if (string.IsNullOrWhiteSpace(fitnessGoal))
            {
                ViewBag.Error =
                    "Please select your fitness goal.";

                return View("AIMeal");
            }

            var validGoals = new[]
            {
                "General fitness",
                "Strength",
                "Endurance",
                "Mobility and flexibility"
            };

            if (!validGoals.Contains(fitnessGoal))
            {
                ViewBag.Error =
                    "Please select a valid fitness goal.";

                return View("AIMeal");
            }

            string? apiKey =
                _configuration["GeminiApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                ViewBag.Error =
                    "The Fitness AI service is not configured. GeminiApiKey was not found.";

                return View("AIMeal");
            }

            apiKey = apiKey.Trim();

            string preferences =
                string.IsNullOrWhiteSpace(foodPreferences)
                    ? "No specific food preferences were provided."
                    : foodPreferences.Trim();

            string diet =
                string.IsNullOrWhiteSpace(dietaryPreference)
                    ? "No specific dietary preference was provided."
                    : dietaryPreference.Trim();

            string prompt =
                "You are the Meal Suggestions AI assistant for DUT Campus FIT Gym.\n\n" +
                "Create a balanced and practical set of general meal suggestions.\n\n" +
                $"Fitness goal: {fitnessGoal}\n" +
                $"Food preferences: {preferences}\n" +
                $"Dietary preference: {diet}\n\n" +
                "Generate exactly four meal suggestions:\n" +
                "- Breakfast\n" +
                "- Lunch\n" +
                "- Snack\n" +
                "- Dinner\n\n" +
                "Rules:\n" +
                "- Suggest balanced and practical meals.\n" +
                "- Use commonly available foods where possible.\n" +
                "- Respect the user's food and dietary preferences where possible.\n" +
                "- Do not recommend extreme diets.\n" +
                "- Do not recommend fasting plans.\n" +
                "- Do not prescribe restrictive calorie limits.\n" +
                "- Do not provide medical nutrition treatment.\n" +
                "- Do not diagnose medical conditions.\n" +
                "- Keep ingredient lists practical.\n" +
                "- Keep preparation instructions short and simple.\n" +
                "- Do not include calorie targets.\n" +
                "- Return only the four meal suggestions.";

            var responseSchema = new
            {
                type = "array",
                minItems = 4,
                maxItems = 4,
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        mealType = new
                        {
                            type = "string"
                        },
                        mealName = new
                        {
                            type = "string"
                        },
                        description = new
                        {
                            type = "string"
                        },
                        ingredients = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "string"
                            }
                        },
                        preparation = new
                        {
                            type = "string"
                        }
                    },
                    required = new[]
                    {
                        "mealType",
                        "mealName",
                        "description",
                        "ingredients",
                        "preparation"
                    }
                }
            };

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = prompt
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    responseSchema = responseSchema
                }
            };

            string json =
                JsonSerializer.Serialize(requestBody);

            var client =
                _httpClientFactory.CreateClient();

            string url =
                "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash-lite:generateContent";

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    url);

            request.Headers.Add(
                "x-goog-api-key",
                apiKey);

            request.Content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

            try
            {
                var response =
                    await client.SendAsync(request);

                string responseContent =
                    await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error =
                        $"Gemini error {(int)response.StatusCode}: {responseContent}";

                    return View("AIMeal");
                }

                using JsonDocument document =
                    JsonDocument.Parse(responseContent);

                if (!document.RootElement.TryGetProperty(
                        "candidates",
                        out JsonElement candidates) ||
                    candidates.GetArrayLength() == 0)
                {
                    ViewBag.Error =
                        "Gemini did not return any meal suggestions.";

                    return View("AIMeal");
                }

                var candidate =
                    candidates[0];

                if (!candidate.TryGetProperty(
                        "content",
                        out JsonElement content) ||
                    !content.TryGetProperty(
                        "parts",
                        out JsonElement parts))
                {
                    ViewBag.Error =
                        "Gemini returned a response without usable meal content.";

                    return View("AIMeal");
                }

                string aiText = "";

                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty(
                            "text",
                            out JsonElement text))
                    {
                        aiText =
                            text.GetString() ?? "";

                        break;
                    }
                }

                if (string.IsNullOrWhiteSpace(aiText))
                {
                    ViewBag.Error =
                        "Gemini returned an empty meal plan.";

                    return View("AIMeal");
                }

                aiText = aiText
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                var meals =
                    JsonSerializer.Deserialize<
                        List<AIMealSuggestionViewModel>>(
                            aiText,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                if (meals == null ||
                    meals.Count != 4)
                {
                    ViewBag.Error =
                        "Gemini returned an invalid meal plan. Please try again.";

                    return View("AIMeal");
                }

                var validMealTypes = new[]
                {
                    "Breakfast",
                    "Lunch",
                    "Snack",
                    "Dinner"
                };

                meals =
                    meals
                        .Where(m =>
                            !string.IsNullOrWhiteSpace(m.MealType) &&
                            !string.IsNullOrWhiteSpace(m.MealName) &&
                            !string.IsNullOrWhiteSpace(m.Description) &&
                            !string.IsNullOrWhiteSpace(m.Preparation) &&
                            m.Ingredients != null &&
                            m.Ingredients.Count > 0 &&
                            validMealTypes.Contains(
                                m.MealType.Trim(),
                                StringComparer.OrdinalIgnoreCase))
                        .ToList();

                if (meals.Count != 4)
                {
                    ViewBag.Error =
                        "Gemini generated an invalid meal structure. Please try again.";

                    return View("AIMeal");
                }

                var mealTypes =
                    meals
                        .Select(m =>
                            m.MealType.Trim())
                        .Distinct(
                            StringComparer.OrdinalIgnoreCase)
                        .Count();

                if (mealTypes != 4)
                {
                    ViewBag.Error =
                        "Gemini did not generate all four required meal types. Please try again.";

                    return View("AIMeal");
                }

                ViewBag.FitnessGoal =
                    fitnessGoal;

                ViewBag.FoodPreferences =
                    preferences;

                ViewBag.DietaryPreference =
                    diet;

                return View(
                    "AIMealResult",
                    meals);
            }
            catch (JsonException ex)
            {
                ViewBag.Error =
                    $"JSON processing error: {ex.Message}";

                return View("AIMeal");
            }
            catch (HttpRequestException ex)
            {
                ViewBag.Error =
                    $"HTTP request error: {ex.Message}";

                return View("AIMeal");
            }
            catch (TaskCanceledException ex)
            {
                ViewBag.Error =
                    $"The Gemini request timed out: {ex.Message}";

                return View("AIMeal");
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    $"Unexpected error: {ex.Message}";

                return View("AIMeal");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveMealPlan(
            string fitnessGoal,
            string foodPreferences,
            string dietaryPreference,
            List<AIMealSuggestionViewModel> meals)
        {
            var memberId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberId))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            if (meals == null ||
                meals.Count != 4)
            {
                return RedirectToAction(
                    "AIMeal");
            }

            var validGoals = new[]
            {
                "General fitness",
                "Strength",
                "Endurance",
                "Mobility and flexibility"
            };

            var validMealTypes = new[]
            {
                "Breakfast",
                "Lunch",
                "Snack",
                "Dinner"
            };

            if (!validGoals.Contains(fitnessGoal))
            {
                return RedirectToAction(
                    "AIMeal");
            }

            var validMeals =
                meals
                    .Where(m =>
                        !string.IsNullOrWhiteSpace(m.MealType) &&
                        !string.IsNullOrWhiteSpace(m.MealName) &&
                        !string.IsNullOrWhiteSpace(m.Description) &&
                        !string.IsNullOrWhiteSpace(m.Preparation) &&
                        m.Ingredients != null &&
                        m.Ingredients.Count > 0 &&
                        validMealTypes.Contains(
                            m.MealType.Trim(),
                            StringComparer.OrdinalIgnoreCase))
                    .ToList();

            if (validMeals.Count != 4)
            {
                return RedirectToAction(
                    "AIMeal");
            }

            var mealTypes =
                validMeals
                    .Select(m =>
                        m.MealType.Trim())
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .Count();

            if (mealTypes != 4)
            {
                return RedirectToAction(
                    "AIMeal");
            }

            int currentMemberId =
                int.Parse(memberId);

            var mealPlan =
                new MealPlan
                {
                    MemberId =
                        currentMemberId,

                    FitnessGoal =
                        fitnessGoal.Trim(),

                    DietaryPreference =
                        dietaryPreference?.Trim() ?? "",

                    FoodPreferences =
                        foodPreferences?.Trim() ?? "",

                    CreatedAt =
                        DateTime.Now
                };

            foreach (var meal in validMeals)
            {
                var item =
                    new MealPlanItem
                    {
                        MealType =
                            meal.MealType.Trim(),

                        MealName =
                            meal.MealName.Trim(),

                        Description =
                            meal.Description.Trim(),

                        Ingredients =
                            string.Join(
                                "\n",
                                meal.Ingredients
                                    .Where(i =>
                                        !string.IsNullOrWhiteSpace(i))
                                    .Select(i =>
                                        i.Trim())),

                        Preparation =
                            meal.Preparation.Trim()
                    };

                mealPlan.MealPlanItems.Add(item);
            }

            _context.MealPlans.Add(
                mealPlan);

            _context.SaveChanges();

            return RedirectToAction(
                "MyMealPlans");
        }

        [HttpGet]
        public IActionResult MyMealPlans()
        {
            var memberId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberId))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            int currentMemberId =
                int.Parse(memberId);

            var mealPlans =
                _context.MealPlans
                    .Where(m =>
                        m.MemberId == currentMemberId)
                    .OrderByDescending(m =>
                        m.CreatedAt)
                    .ToList();

            return View(mealPlans);
        }

        [HttpGet]
        public IActionResult MealPlanDetails(int id)
        {
            var memberId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberId))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            int currentMemberId =
                int.Parse(memberId);

            var mealPlan =
                _context.MealPlans
                    .Include(m =>
                        m.MealPlanItems)
                    .FirstOrDefault(m =>
                        m.MealPlanId == id &&
                        m.MemberId == currentMemberId);

            if (mealPlan == null)
            {
                return NotFound();
            }

            return View(mealPlan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteMealPlan(int id)
        {
            var memberId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberId))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            int currentMemberId =
                int.Parse(memberId);

            var mealPlan =
                _context.MealPlans
                    .FirstOrDefault(m =>
                        m.MealPlanId == id &&
                        m.MemberId == currentMemberId);

            if (mealPlan != null)
            {
                _context.MealPlans.Remove(
                    mealPlan);

                _context.SaveChanges();
            }

            return RedirectToAction(
                "MyMealPlans");
        }
    }
}