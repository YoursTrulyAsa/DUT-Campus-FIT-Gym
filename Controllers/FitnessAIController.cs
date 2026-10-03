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
        public async Task<IActionResult> AIWorkout()
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
                return RedirectToAction("Login", "Account");

            var profile =
                await _context.WorkoutProfiles
                    .FirstOrDefaultAsync(p => p.MemberId == memberId);

            if (profile == null)
            {
                return RedirectToAction(
                    "Create",
                    "WorkoutProfile");
            }

            var validLevels = new[]
            {
                "Beginner",
                "Intermediate",
                "Pro"
            };

            var validGoals = new[]
            {
                "General fitness",
                "Strength",
                "Endurance",
                "Mobility and flexibility"
            };

            var fitnessLevel =
                validLevels.FirstOrDefault(
                    l => l.Equals(
                        profile.FitnessLevel?.Trim(),
                        StringComparison.OrdinalIgnoreCase));

            var fitnessGoal =
                validGoals.FirstOrDefault(
                    g => g.Equals(
                        profile.Goal?.Trim(),
                        StringComparison.OrdinalIgnoreCase));

            if (fitnessLevel == null || fitnessGoal == null)
            {
                ViewBag.Error =
                    "Your Fitness Profile contains an invalid fitness level or goal. Please edit your Fitness Profile and select valid options.";

                return View();
            }

            ViewBag.FitnessLevel = fitnessLevel;
            ViewBag.FitnessGoal = fitnessGoal;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateWorkout(
            string preferredExercises,
            int daysAvailable)
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
                return RedirectToAction("Login", "Account");

            if (daysAvailable < 2 || daysAvailable > 5)
            {
                ViewBag.Error =
                    "Please select between 2 and 5 workout days per week.";

                return View("AIWorkout");
            }

            var profile =
                await _context.WorkoutProfiles
                    .FirstOrDefaultAsync(p => p.MemberId == memberId);

            if (profile == null)
            {
                ViewBag.Error =
                    "Please complete your Fitness Profile before creating an AI programme.";

                return RedirectToAction(
                    "Create",
                    "WorkoutProfile");
            }

            var validLevels = new[]
            {
                "Beginner",
                "Intermediate",
                "Pro"
            };

            var validGoals = new[]
            {
                "General fitness",
                "Strength",
                "Endurance",
                "Mobility and flexibility"
            };

            var fitnessLevel =
                validLevels.FirstOrDefault(
                    l => l.Equals(
                        profile.FitnessLevel?.Trim(),
                        StringComparison.OrdinalIgnoreCase));

            var fitnessGoal =
                validGoals.FirstOrDefault(
                    g => g.Equals(
                        profile.Goal?.Trim(),
                        StringComparison.OrdinalIgnoreCase));

            if (fitnessLevel == null || fitnessGoal == null)
            {
                ViewBag.Error =
                    "Your Fitness Profile contains an invalid fitness level or goal. Please edit your Fitness Profile and try again.";

                return View("AIWorkout");
            }

            var exerciseLibrary =
                await _context.Exercises
                    .OrderBy(e => e.ExerciseId)
                    .ToListAsync();

            if (exerciseLibrary.Count == 0)
            {
                ViewBag.Error =
                    "The gym exercise library is currently empty.";

                return View("AIWorkout");
            }

            string exerciseLibraryText =
                string.Join(
                    "\n",
                    exerciseLibrary.Select(
                        e =>
                            $"- {e.ExerciseName} | Difficulty: {e.Difficulty} | Category: {e.Category} | Muscle group: {e.MuscleGroup}"));

            string exercisePreference =
                string.IsNullOrWhiteSpace(preferredExercises)
                    ? "No specific exercise preference."
                    : preferredExercises.Trim();

            string prompt =
                "You are the Fitness AI assistant for DUT Campus FIT Gym.\n\n" +
                "Create a safe, progressive four-week workout programme for a gym member.\n\n" +
                $"Fitness level: {fitnessLevel}\n" +
                $"Fitness goal: {fitnessGoal}\n" +
                $"Workout days per week: {daysAvailable}\n" +
                $"Preferred exercises: {exercisePreference}\n\n" +
                "The programme must contain exactly 4 weeks.\n" +
                $"Each week must contain exactly {daysAvailable} distinct workout days.\n" +
                "Each workout day should contain between 2 and 4 exercises.\n\n" +
                "IMPORTANT EXERCISE RULE:\n" +
                "You MUST use exercise names exactly as they appear in the supplied gym exercise library.\n" +
                "Do not invent, rename, abbreviate, pluralise, or substitute exercise names.\n\n" +
                "AVAILABLE GYM EXERCISES:\n" +
                exerciseLibraryText +
                "\n\n" +
                "PROGRESSION RULES:\n" +
                "- Week 1 should establish a manageable starting workload.\n" +
                "- Week 2 may make a small progression where appropriate.\n" +
                "- Week 3 may provide another reasonable progression.\n" +
                "- Week 4 should remain challenging but manageable.\n" +
                "- Do not use extreme exercise volume.\n" +
                "- Do not create dangerous challenges.\n" +
                "- Respect the member's fitness level.\n" +
                "- Respect the member's fitness goal.\n" +
                "- Include recovery days by leaving days outside the selected workout days.\n" +
                "- Do not include warm-ups or cool-downs as exercises.\n" +
                "- Sets must be between 1 and 5.\n" +
                "- Repetitions must be between 1 and 20.\n" +
                "- RestTime must be between 30 and 180 seconds.\n" +
                "- Keep descriptions short and practical.\n" +
                "- Do not provide medical treatment or injury rehabilitation.\n\n" +
                "Return only the programme exercises.";

            var responseSchema = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        weekNumber = new
                        {
                            type = "integer"
                        },
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
                            type = "integer"
                        },
                        repetitions = new
                        {
                            type = "integer"
                        },
                        restTime = new
                        {
                            type = "integer"
                        },
                        description = new
                        {
                            type = "string"
                        }
                    },
                    required = new[]
                    {
                        "weekNumber",
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
                _configuration["GeminiApiKey"]?.Trim() ?? "");

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
                        "Gemini did not return any programme data.";

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
                        "Gemini returned a response without usable programme content.";

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
                        "Gemini returned an empty programme.";

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
                        "Gemini returned an empty or invalid programme.";

                    return View("AIWorkout");
                }

                if (exercises.Any(e =>
                        e.WeekNumber < 1 ||
                        e.WeekNumber > 4 ||
                        string.IsNullOrWhiteSpace(e.WorkoutDay) ||
                        string.IsNullOrWhiteSpace(e.ExerciseName) ||
                        e.Sets < 1 ||
                        e.Sets > 5 ||
                        e.Repetitions < 1 ||
                        e.Repetitions > 20 ||
                        e.RestTime < 30 ||
                        e.RestTime > 180))
                {
                    ViewBag.Error =
                        "Gemini generated exercises outside the allowed limits.";

                    return View("AIWorkout");
                }

                var validExerciseNames =
                    exerciseLibrary
                        .Select(e => e.ExerciseName)
                        .ToHashSet(
                            StringComparer.OrdinalIgnoreCase);

                var invalidExercises =
                    exercises
                        .Where(e =>
                            !validExerciseNames.Contains(
                                e.ExerciseName.Trim()))
                        .Select(e =>
                            e.ExerciseName.Trim())
                        .Distinct(
                            StringComparer.OrdinalIgnoreCase)
                        .ToList();

                if (invalidExercises.Any())
                {
                    ViewBag.Error =
                        "Gemini generated exercises that are not available in the gym exercise library. Please try again.";

                    return View("AIWorkout");
                }

                for (int week = 1; week <= 4; week++)
                {
                    var weekExercises =
                        exercises
                            .Where(e => e.WeekNumber == week)
                            .ToList();

                    var weekDays =
                        weekExercises
                            .Select(e => e.WorkoutDay.Trim())
                            .Distinct(
                                StringComparer.OrdinalIgnoreCase)
                            .Count();

                    if (weekDays != daysAvailable)
                    {
                        ViewBag.Error =
                            $"Week {week} contains {weekDays} workout days instead of {daysAvailable}. Please try again.";

                        return View("AIWorkout");
                    }
                }

                ViewBag.FitnessLevel = fitnessLevel;
                ViewBag.FitnessGoal = fitnessGoal;
                ViewBag.DaysAvailable = daysAvailable;

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
        public async Task<IActionResult> SaveWorkout(
    List<AIWorkoutExerciseViewModel> exercises,
    string fitnessLevel,
    string fitnessGoal,
    int daysAvailable)
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int currentMemberId))
                return RedirectToAction("Login", "Account");

            if (exercises == null || exercises.Count == 0)
                return RedirectToAction("AIWorkout");

            var profile =
                await _context.WorkoutProfiles
                    .FirstOrDefaultAsync(
                        p => p.MemberId == currentMemberId);

            if (profile == null)
            {
                return RedirectToAction(
                    "Create",
                    "WorkoutProfile");
            }

            var validLevels = new[]
            {
        "Beginner",
        "Intermediate",
        "Pro"
    };

            var validGoals = new[]
            {
        "General fitness",
        "Strength",
        "Endurance",
        "Mobility and flexibility"
    };

            var actualLevel =
                validLevels.FirstOrDefault(
                    l => l.Equals(
                        profile.FitnessLevel?.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                ?? "Beginner";

            var actualGoal =
                validGoals.FirstOrDefault(
                    g => g.Equals(
                        profile.Goal?.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                ?? "General fitness";

            if (daysAvailable < 2 || daysAvailable > 5)
                return RedirectToAction("AIWorkout");

            if (exercises.Any(e =>
                    e.WeekNumber < 1 ||
                    e.WeekNumber > 4 ||
                    string.IsNullOrWhiteSpace(e.WorkoutDay) ||
                    string.IsNullOrWhiteSpace(e.ExerciseName) ||
                    e.Sets < 1 ||
                    e.Sets > 5 ||
                    e.Repetitions < 1 ||
                    e.Repetitions > 20 ||
                    e.RestTime < 30 ||
                    e.RestTime > 180))
            {
                return RedirectToAction("AIWorkout");
            }

            var exerciseLibrary =
                await _context.Exercises.ToListAsync();

            if (exerciseLibrary.Count == 0)
                return RedirectToAction("AIWorkout");

            var programme =
                new WorkoutProgramme
                {
                    MemberId = currentMemberId,
                    ProgrammeName =
                        $"{actualLevel} {actualGoal} Programme",
                    FitnessLevel = actualLevel,
                    Goal = actualGoal,
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today.AddDays(27),
                    IsCompleted = false,
                    CreatedAt = DateTime.Now
                };

            _context.WorkoutProgrammes.Add(programme);

            await _context.SaveChangesAsync();

            foreach (var exercise in exercises)
            {
                var libraryExercise =
                    exerciseLibrary.FirstOrDefault(
                        e => e.ExerciseName.Equals(
                            exercise.ExerciseName.Trim(),
                            StringComparison.OrdinalIgnoreCase));

                if (libraryExercise == null)
                    continue;

                _context.WorkoutPlans.Add(
    new WorkoutPlan
    {
        MemberId = currentMemberId,
        WorkoutProgrammeId =
            programme.WorkoutProgrammeId,
        WorkoutName =
            programme.ProgrammeName,
        ExerciseName =
            libraryExercise.ExerciseName,
        Level = actualLevel,
        WorkoutDay =
            exercise.WorkoutDay.Trim(),
        WeekNumber =
            exercise.WeekNumber,
        ExerciseId =
            libraryExercise.ExerciseId,
        Sets =
            Math.Clamp(
                exercise.Sets,
                1,
                5),
        Repetitions =
            Math.Clamp(
                exercise.Repetitions,
                1,
                20),
        RestTime =
            Math.Clamp(
                exercise.RestTime,
                30,
                180),
        Description =
            string.IsNullOrWhiteSpace(
                exercise.Description)
                ? libraryExercise.Description
                : exercise.Description.Trim()
    });
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "MyWorkout",
                "Workout");
        }

        [HttpGet]
        public async Task<IActionResult> AIMeal()
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var profile =
                await _context.WorkoutProfiles
                    .FirstOrDefaultAsync(
                        p => p.MemberId == memberId);

            if (profile == null)
            {
                return RedirectToAction(
                    "Create",
                    "WorkoutProfile");
            }

            var validLevels = new[]
            {
                "Beginner",
                "Intermediate",
                "Pro"
            };

            var validGoals = new[]
            {
                "General fitness",
                "Strength",
                "Endurance",
                "Mobility and flexibility"
            };

            var fitnessLevel =
                validLevels.FirstOrDefault(
                    level =>
                        level.Equals(
                            profile.FitnessLevel?.Trim(),
                            StringComparison.OrdinalIgnoreCase));

            var fitnessGoal =
                validGoals.FirstOrDefault(
                    goal =>
                        goal.Equals(
                            profile.Goal?.Trim(),
                            StringComparison.OrdinalIgnoreCase));

            if (fitnessLevel == null ||
                fitnessGoal == null)
            {
                ViewBag.Error =
                    "Your Fitness Profile contains an invalid fitness level or goal. Please update your Fitness Profile.";

                return View();
            }

            ViewBag.FitnessLevel = fitnessLevel;
            ViewBag.FitnessGoal = fitnessGoal;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateMealPlan(
            string foodPreferences,
            string dietaryPreference)
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var profile =
                await _context.WorkoutProfiles
                    .FirstOrDefaultAsync(
                        p => p.MemberId == memberId);

            if (profile == null)
            {
                return RedirectToAction(
                    "Create",
                    "WorkoutProfile");
            }

            var validLevels = new[]
            {
                "Beginner",
                "Intermediate",
                "Pro"
            };

            var validGoals = new[]
            {
                "General fitness",
                "Strength",
                "Endurance",
                "Mobility and flexibility"
            };

            var fitnessLevel =
                validLevels.FirstOrDefault(
                    level =>
                        level.Equals(
                            profile.FitnessLevel?.Trim(),
                            StringComparison.OrdinalIgnoreCase));

            var fitnessGoal =
                validGoals.FirstOrDefault(
                    goal =>
                        goal.Equals(
                            profile.Goal?.Trim(),
                            StringComparison.OrdinalIgnoreCase));

            if (fitnessLevel == null ||
                fitnessGoal == null)
            {
                ViewBag.Error =
                    "Your Fitness Profile contains an invalid fitness level or goal. Please update your Fitness Profile.";

                return View("AIMeal");
            }

            string? apiKey =
                _configuration["GeminiApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                ViewBag.Error =
                    "The Fitness AI service is not configured. GeminiApiKey was not found.";

                ViewBag.FitnessLevel = fitnessLevel;
                ViewBag.FitnessGoal = fitnessGoal;

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
                "Create a balanced and practical set of general meal suggestions for a gym member.\n\n" +
                $"Fitness level: {fitnessLevel}\n" +
                $"Fitness goal: {fitnessGoal}\n" +
                $"Food preferences: {preferences}\n" +
                $"Dietary preference: {diet}\n\n" +
                "Use the fitness level when deciding how substantial and practical the meals should be.\n" +
                "Use the fitness goal when deciding which types of foods and meals are useful.\n\n" +
                "Fitness level guidance:\n" +
                "- Beginner: keep meals simple, balanced and practical for someone starting a fitness routine.\n" +
                "- Intermediate: provide balanced meals that reasonably support regular training.\n" +
                "- Pro: provide balanced meals suitable for a member with a higher training workload.\n\n" +
                "Fitness goal guidance:\n" +
                "- General fitness: focus on balanced everyday meals.\n" +
                "- Strength: include practical protein-rich food options alongside balanced carbohydrates and vegetables.\n" +
                "- Endurance: include practical carbohydrate sources and balanced meals suitable for regular endurance activity.\n" +
                "- Mobility and flexibility: focus on balanced meals with varied whole-food choices.\n\n" +
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
                "- Do not include calorie targets.\n" +
                "- Keep ingredient lists practical.\n" +
                "- Keep preparation instructions short and simple.\n" +
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

                    ViewBag.FitnessLevel = fitnessLevel;
                    ViewBag.FitnessGoal = fitnessGoal;

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

                ViewBag.FitnessLevel = fitnessLevel;
                ViewBag.FitnessGoal = fitnessGoal;
                ViewBag.FoodPreferences = preferences;
                ViewBag.DietaryPreference = diet;

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
        public async Task<IActionResult> SaveMealPlan(
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

            int currentMemberId =
                int.Parse(memberId);

            var profile =
                await _context.WorkoutProfiles
                    .FirstOrDefaultAsync(
                        p => p.MemberId == currentMemberId);

            if (profile == null)
            {
                return RedirectToAction(
                    "Create",
                    "WorkoutProfile");
            }

            var validGoals = new[]
            {
                "General fitness",
                "Strength",
                "Endurance",
                "Mobility and flexibility"
            };

            var fitnessGoal =
                validGoals.FirstOrDefault(
                    goal =>
                        goal.Equals(
                            profile.Goal?.Trim(),
                            StringComparison.OrdinalIgnoreCase));

            if (fitnessGoal == null)
            {
                return RedirectToAction(
                    "AIMeal");
            }

            var validMealTypes = new[]
            {
                "Breakfast",
                "Lunch",
                "Snack",
                "Dinner"
            };

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

            var mealPlan =
                new MealPlan
                {
                    MemberId =
                        currentMemberId,

                    FitnessGoal =
                        fitnessGoal,

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

            await _context.SaveChangesAsync();

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