using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.Services;
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
    public class FitnessChallengeController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GymDbContext _context;
        private readonly RewardService _rewardService;

        public FitnessChallengeController(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            GymDbContext context,
            RewardService rewardService)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _context = context;
            _rewardService = rewardService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(
            string level,
            string category)
        {
            var validLevels = new[]
            {
                "Beginner",
                "Intermediate",
                "Pro"
            };

            var validCategories = new[]
            {
                "Cardio",
                "Strength",
                "Yoga"
            };

            if (!validLevels.Contains(
                    level,
                    StringComparer.OrdinalIgnoreCase) ||
                !validCategories.Contains(
                    category,
                    StringComparer.OrdinalIgnoreCase))
            {
                ViewBag.Error =
                    "Please select a valid challenge level and category.";

                return View("Index");
            }

            level =
                validLevels.First(
                    x => x.Equals(
                        level,
                        StringComparison.OrdinalIgnoreCase));

            category =
                validCategories.First(
                    x => x.Equals(
                        category,
                        StringComparison.OrdinalIgnoreCase));

            int rewardPoints =
                _rewardService.GetChallengeReward(level);

            string? apiKey =
                _configuration["GeminiApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                ViewBag.Error =
                    "The Fitness AI service is not configured. GeminiApiKey was not found.";

                return View("Index");
            }

            string prompt =
                "You are the Fitness Challenge AI assistant for DUT Campus FIT Gym.\n\n" +
                "Create one safe, reasonable fitness challenge for a student.\n\n" +
                $"Fitness level: {level}\n" +
                $"Category: {category}\n\n" +
                "Rules:\n" +
                "- The challenge must be realistic and appropriate for the selected level.\n" +
                "- Do not recommend extreme exercise amounts.\n" +
                "- Do not create dangerous or unsafe challenges.\n" +
                "- Beginner challenges must be clearly beginner-appropriate.\n" +
                "- Do not use extreme repetition counts or excessive duration.\n" +
                "- Keep the challenge simple enough to understand and complete.\n" +
                $"- The reward must always be exactly {rewardPoints} points.\n" +
                "- Do not include medical diagnoses.\n" +
                "- Do not prescribe exercise as treatment for injuries or medical conditions.\n" +
                "- Do not include calorie targets.\n" +
                "- Return only one challenge.";

            var responseSchema = new
            {
                type = "object",
                properties = new
                {
                    level = new
                    {
                        type = "string"
                    },
                    category = new
                    {
                        type = "string"
                    },
                    title = new
                    {
                        type = "string"
                    },
                    description = new
                    {
                        type = "string"
                    },
                    target = new
                    {
                        type = "string"
                    },
                    rewardPoints = new
                    {
                        type = "integer",
                        minimum = rewardPoints,
                        maximum = rewardPoints
                    }
                },
                required = new[]
                {
                    "level",
                    "category",
                    "title",
                    "description",
                    "target",
                    "rewardPoints"
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

                    return View("Index");
                }

                using JsonDocument document =
                    JsonDocument.Parse(responseContent);

                if (!document.RootElement.TryGetProperty(
                        "candidates",
                        out JsonElement candidates) ||
                    candidates.GetArrayLength() == 0)
                {
                    ViewBag.Error =
                        "Gemini did not return a challenge.";

                    return View("Index");
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
                        "Gemini returned a response without usable challenge content.";

                    return View("Index");
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
                        "Gemini returned an empty challenge.";

                    return View("Index");
                }

                aiText = aiText
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                var challenge =
                    JsonSerializer.Deserialize<FitnessChallengeViewModel>(
                        aiText,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (challenge == null)
                {
                    ViewBag.Error =
                        "Gemini returned an invalid challenge.";

                    return View("Index");
                }

                challenge.Level =
                    challenge.Level.Trim();

                challenge.Category =
                    challenge.Category.Trim();

                challenge.Title =
                    challenge.Title.Trim();

                challenge.Description =
                    challenge.Description.Trim();

                challenge.Target =
                    challenge.Target.Trim();

                if (!validLevels.Contains(
                        challenge.Level,
                        StringComparer.OrdinalIgnoreCase) ||
                    !validCategories.Contains(
                        challenge.Category,
                        StringComparer.OrdinalIgnoreCase) ||
                    string.IsNullOrWhiteSpace(
                        challenge.Title) ||
                    string.IsNullOrWhiteSpace(
                        challenge.Description) ||
                    string.IsNullOrWhiteSpace(
                        challenge.Target) ||
                    challenge.RewardPoints != rewardPoints)
                {
                    ViewBag.Error =
                        "Gemini generated a challenge outside the allowed limits. Please try again.";

                    return View("Index");
                }

                challenge.Level =
                    level;

                challenge.Category =
                    category;

                challenge.RewardPoints =
                    rewardPoints;

                return View(
                    "Result",
                    challenge);
            }
            catch (JsonException ex)
            {
                ViewBag.Error =
                    $"JSON processing error: {ex.Message}";

                return View("Index");
            }
            catch (HttpRequestException ex)
            {
                ViewBag.Error =
                    $"HTTP request error: {ex.Message}";

                return View("Index");
            }
            catch (TaskCanceledException ex)
            {
                ViewBag.Error =
                    $"The Gemini request timed out: {ex.Message}";

                return View("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    $"Unexpected error: {ex.Message}";

                return View("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Accept(
            FitnessChallengeViewModel challenge)
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

            var validLevels = new[]
            {
                "Beginner",
                "Intermediate",
                "Pro"
            };

            var validCategories = new[]
            {
                "Cardio",
                "Strength",
                "Yoga"
            };

            if (!validLevels.Contains(
                    challenge.Level,
                    StringComparer.OrdinalIgnoreCase) ||
                !validCategories.Contains(
                    challenge.Category,
                    StringComparer.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(
                    challenge.Title) ||
                string.IsNullOrWhiteSpace(
                    challenge.Description) ||
                string.IsNullOrWhiteSpace(
                    challenge.Target))
            {
                return RedirectToAction("Index");
            }

            string level =
                validLevels.First(
                    x => x.Equals(
                        challenge.Level.Trim(),
                        StringComparison.OrdinalIgnoreCase));

            string category =
                validCategories.First(
                    x => x.Equals(
                        challenge.Category.Trim(),
                        StringComparison.OrdinalIgnoreCase));

            int rewardPoints =
                _rewardService.GetChallengeReward(level);

            int currentMemberId =
                int.Parse(memberId);

            var fitnessChallenge =
                new FitnessChallenge
                {
                    MemberId =
                        currentMemberId,

                    Level =
                        level,

                    Category =
                        category,

                    Title =
                        challenge.Title.Trim(),

                    Description =
                        challenge.Description.Trim(),

                    Target =
                        challenge.Target.Trim(),

                    RewardPoints =
                        rewardPoints,

                    Status =
                        "Accepted",

                    CreatedAt =
                        DateTime.Now
                };

            var participation =
                new ChallengeParticipation
                {
                    MemberId =
                        currentMemberId,

                    FitnessChallenge =
                        fitnessChallenge,

                    AcceptedAt =
                        DateTime.Now,

                    Status =
                        "Accepted"
                };

            _context.FitnessChallenges.Add(
                fitnessChallenge);

            _context.ChallengeParticipations.Add(
                participation);

            _context.SaveChanges();

            return RedirectToAction(
                "MyChallenges");
        }

        [HttpGet]
        public IActionResult MyChallenges()
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

            var challenges =
                _context.FitnessChallenges
                    .Include(c => c.Participation)
                    .Where(c =>
                        c.MemberId == currentMemberId)
                    .OrderByDescending(c =>
                        c.CreatedAt)
                    .ToList();

            return View(challenges);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Complete(
            int id)
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

            var challenge =
                _context.FitnessChallenges
                    .Include(c => c.Participation)
                    .FirstOrDefault(c =>
                        c.FitnessChallengeId == id &&
                        c.MemberId == currentMemberId);

            if (challenge == null ||
                challenge.Participation == null)
            {
                return RedirectToAction("MyChallenges");
            }

            if (challenge.Participation.Status == "Completed")
            {
                return RedirectToAction("MyChallenges");
            }

            var rewardPoints =
                _rewardService.GetChallengeReward(
                    challenge.Level);

            challenge.Status =
                "Completed";

            challenge.Participation.Status =
                "Completed";

            challenge.Participation.CompletedAt =
                DateTime.Now;

            var alreadyRewarded =
                _context.RewardPoints.Any(r =>
                    r.MemberId == currentMemberId &&
                    r.Reason ==
                        $"Completed fitness challenge: {challenge.FitnessChallengeId}");

            if (!alreadyRewarded)
            {
                _context.RewardPoints.Add(
                    new RewardPoint
                    {
                        MemberId =
                            currentMemberId,

                        Points =
                            rewardPoints,

                        Reason =
                            $"Completed fitness challenge: {challenge.FitnessChallengeId}",

                        EarnedAt =
                            DateTime.Now
                    });
            }

            _context.SaveChanges();

            return RedirectToAction(
                "MyChallenges");
        }
    }
}