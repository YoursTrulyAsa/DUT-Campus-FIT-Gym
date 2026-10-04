using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.Services;
using DUT_Campus_FIT_Gym.ViewModels;
using FFMpegCore;
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
        private readonly IWebHostEnvironment _environment;

        private const long MaxVideoSize =
            500L * 1024L * 1024L;

        private static readonly TimeSpan MinimumVideoDuration =
            TimeSpan.FromSeconds(290);

        private static readonly TimeSpan MaximumVideoDuration =
            TimeSpan.FromSeconds(310);

        public FitnessChallengeController(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            GymDbContext context,
            RewardService rewardService,
            IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _context = context;
            _rewardService = rewardService;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
            {
                return RedirectToAction("Login", "Account");
            }

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

            var fitnessLevel =
                validLevels.FirstOrDefault(
                    level =>
                        level.Equals(
                            profile.FitnessLevel?.Trim(),
                            StringComparison.OrdinalIgnoreCase));

            if (fitnessLevel == null)
            {
                ViewBag.Error =
                    "Your Fitness Profile does not contain a valid fitness level. Please update your Fitness Profile.";

                return View();
            }

            ViewBag.FitnessLevel = fitnessLevel;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(
            string category)
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
            {
                return RedirectToAction("Login", "Account");
            }

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

            var validCategories = new[]
            {
                "Cardio",
                "Strength",
                "Yoga"
            };

            var fitnessLevel =
                validLevels.FirstOrDefault(
                    level =>
                        level.Equals(
                            profile.FitnessLevel?.Trim(),
                            StringComparison.OrdinalIgnoreCase));

            if (fitnessLevel == null)
            {
                ViewBag.Error =
                    "Your Fitness Profile contains an invalid fitness level. Please update your Fitness Profile.";

                return View("Index");
            }

            if (!validCategories.Contains(
                    category,
                    StringComparer.OrdinalIgnoreCase))
            {
                ViewBag.Error =
                    "Please select a valid challenge category.";

                ViewBag.FitnessLevel =
                    fitnessLevel;

                return View("Index");
            }

            category =
                validCategories.First(
                    x => x.Equals(
                        category,
                        StringComparison.OrdinalIgnoreCase));

            int rewardPoints =
                _rewardService.GetChallengeReward(
                    fitnessLevel);

            string? apiKey =
                _configuration["GeminiApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                ViewBag.Error =
                    "The Fitness AI service is not configured. GeminiApiKey was not found.";

                ViewBag.FitnessLevel =
                    fitnessLevel;

                return View("Index");
            }

            string prompt =
                "Create exactly one safe fitness challenge for a DUT Campus FIT Gym student.\n" +
                $"Fitness level: {fitnessLevel}\n" +
                $"Category: {category}\n\n" +
                "Requirements:\n" +
                "- The challenge MUST be appropriate for the member's fitness level.\n" +
                "- Beginner challenges must use manageable beginner-level activity.\n" +
                "- Intermediate challenges may use moderate progression.\n" +
                "- Pro challenges may use more demanding but still reasonable activity.\n" +
                "- Simple and realistic.\n" +
                "- No extreme exercise amounts.\n" +
                "- No dangerous activities.\n" +
                "- No medical treatment or diagnoses.\n" +
                "- No calorie targets.\n" +
                "- Reward points must be exactly the supplied value.\n" +
                "- Keep the title short.\n" +
                "- Keep the description to one or two sentences.\n" +
                "- Keep the target short and measurable.\n" +
                "- Return only the requested JSON object.";

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
                    responseSchema = responseSchema,
                    maxOutputTokens = 300
                }
            };

            string json =
                JsonSerializer.Serialize(requestBody);

            var client =
                _httpClientFactory.CreateClient();

            client.Timeout =
                TimeSpan.FromSeconds(20);

            string url =
                "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash-lite:generateContent";

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    url);

            request.Headers.Add(
                "x-goog-api-key",
                apiKey.Trim());

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

                    ViewBag.FitnessLevel =
                        fitnessLevel;

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

                    ViewBag.FitnessLevel =
                        fitnessLevel;

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

                    ViewBag.FitnessLevel =
                        fitnessLevel;

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

                    ViewBag.FitnessLevel =
                        fitnessLevel;

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

                    ViewBag.FitnessLevel =
                        fitnessLevel;

                    return View("Index");
                }

                challenge.Level =
                    challenge.Level?.Trim() ?? "";

                challenge.Category =
                    challenge.Category?.Trim() ?? "";

                challenge.Title =
                    challenge.Title?.Trim() ?? "";

                challenge.Description =
                    challenge.Description?.Trim() ?? "";

                challenge.Target =
                    challenge.Target?.Trim() ?? "";

                if (!validCategories.Contains(
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

                    ViewBag.FitnessLevel =
                        fitnessLevel;

                    return View("Index");
                }

                challenge.Level =
                    fitnessLevel;

                challenge.Category =
                    category;

                challenge.RewardPoints =
                    rewardPoints;

                return View(
                    "Result",
                    challenge);
            }
            catch (TaskCanceledException)
            {
                ViewBag.Error =
                    "The Fitness AI request took too long. Please try again.";

                ViewBag.FitnessLevel =
                    fitnessLevel;

                return View("Index");
            }
            catch (JsonException ex)
            {
                ViewBag.Error =
                    $"JSON processing error: {ex.Message}";

                ViewBag.FitnessLevel =
                    fitnessLevel;

                return View("Index");
            }
            catch (HttpRequestException ex)
            {
                ViewBag.Error =
                    $"HTTP request error: {ex.Message}";

                ViewBag.FitnessLevel =
                    fitnessLevel;

                return View("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    $"Unexpected error: {ex.Message}";

                ViewBag.FitnessLevel =
                    fitnessLevel;

                return View("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(
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

            var actualLevel =
                validLevels.FirstOrDefault(
                    level =>
                        level.Equals(
                            profile.FitnessLevel?.Trim(),
                            StringComparison.OrdinalIgnoreCase));

            if (actualLevel == null)
            {
                return RedirectToAction("Index");
            }

            if (!validCategories.Contains(
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

            string category =
                validCategories.First(
                    x => x.Equals(
                        challenge.Category.Trim(),
                        StringComparison.OrdinalIgnoreCase));

            int rewardPoints =
                _rewardService.GetChallengeReward(
                    actualLevel);

            var fitnessChallenge =
                new FitnessChallenge
                {
                    MemberId =
                        currentMemberId,

                    Level =
                        actualLevel,

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

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "MyChallenges");
        }

        [HttpGet]
        public async Task<IActionResult> MyChallenges()
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
                await _context.FitnessChallenges
                    .Include(c => c.Participation)
                    .Include(c => c.Member)
                    .Where(c =>
                        c.MemberId == currentMemberId)
                    .OrderByDescending(c =>
                        c.CreatedAt)
                    .ToListAsync();

            var challengeIds =
                challenges
                    .Select(c => c.FitnessChallengeId)
                    .ToList();

            var proofs =
                await _context.ChallengeProofs
                    .Where(p =>
                        p.MemberId == currentMemberId &&
                        challengeIds.Contains(
                            p.FitnessChallengeId))
                    .OrderByDescending(p =>
                        p.SubmittedAt)
                    .ToListAsync();

            ViewBag.Proofs =
                proofs
                    .GroupBy(p => p.FitnessChallengeId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First());

            return View(challenges);
        }

        [HttpGet]
        public async Task<IActionResult> SubmitProof(int id)
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
                await _context.FitnessChallenges
                    .Include(c => c.Participation)
                    .Include(c => c.Member)
                    .FirstOrDefaultAsync(c =>
                        c.FitnessChallengeId == id &&
                        c.MemberId == currentMemberId);

            if (challenge == null ||
                challenge.Participation == null)
            {
                return RedirectToAction("MyChallenges");
            }

            if (challenge.Participation.Status == "Completed")
            {
                TempData["ChallengeError"] =
                    "This challenge has already been completed.";

                return RedirectToAction(
                    "MyChallenges");
            }

            var existingProof =
                await _context.ChallengeProofs
                    .Where(p =>
                        p.FitnessChallengeId == id &&
                        p.MemberId == currentMemberId)
                    .OrderByDescending(p =>
                        p.SubmittedAt)
                    .FirstOrDefaultAsync();

            if (existingProof != null &&
                existingProof.Status == "PendingReview")
            {
                TempData["ChallengeError"] =
                    "Your proof is already waiting for trainer review.";

                return RedirectToAction(
                    "MyChallenges");
            }

            ViewBag.Challenge =
                challenge;

            ViewBag.ExistingProof =
                existingProof;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MaxVideoSize)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxVideoSize)]
        public async Task<IActionResult> SubmitProof(
            int id,
            IFormFile video)
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
                await _context.FitnessChallenges
                    .Include(c => c.Participation)
                    .FirstOrDefaultAsync(c =>
                        c.FitnessChallengeId == id &&
                        c.MemberId == currentMemberId);

            if (challenge == null ||
                challenge.Participation == null)
            {
                TempData["ChallengeError"] =
                    "Challenge not found.";

                return RedirectToAction(
                    "MyChallenges");
            }

            if (challenge.Participation.Status == "Completed")
            {
                TempData["ChallengeError"] =
                    "This challenge has already been completed.";

                return RedirectToAction(
                    "MyChallenges");
            }

            var existingPendingProof =
                await _context.ChallengeProofs
                    .AnyAsync(p =>
                        p.FitnessChallengeId == id &&
                        p.MemberId == currentMemberId &&
                        p.Status == "PendingReview");

            if (existingPendingProof)
            {
                TempData["ChallengeError"] =
                    "You already have a proof submission waiting for trainer review.";

                return RedirectToAction(
                    "MyChallenges");
            }

            if (video == null ||
                video.Length == 0)
            {
                TempData["ChallengeError"] =
                    "Please select a video to upload.";

                return RedirectToAction(
                    "SubmitProof",
                    new { id });
            }

            if (video.Length > MaxVideoSize)
            {
                TempData["ChallengeError"] =
                    "The video file is too large. The maximum allowed file size is 500 MB.";

                return RedirectToAction(
                    "SubmitProof",
                    new { id });
            }

            var allowedExtensions =
                new[]
                {
                    ".mp4",
                    ".mov",
                    ".webm",
                    ".avi",
                    ".mkv"
                };

            var extension =
                Path.GetExtension(
                    video.FileName)
                    .ToLowerInvariant();

            if (!allowedExtensions.Contains(
                    extension))
            {
                TempData["ChallengeError"] =
                    "Invalid video format. Please upload MP4, MOV, WEBM, AVI, or MKV.";

                return RedirectToAction(
                    "SubmitProof",
                    new { id });
            }

            var allowedContentTypes =
                new[]
                {
                    "video/mp4",
                    "video/quicktime",
                    "video/webm",
                    "video/x-msvideo",
                    "video/x-matroska"
                };

            if (!string.IsNullOrWhiteSpace(video.ContentType) &&
                !allowedContentTypes.Contains(
                    video.ContentType.ToLowerInvariant()))
            {
                TempData["ChallengeError"] =
                    "The uploaded file does not appear to be a valid video.";

                return RedirectToAction(
                    "SubmitProof",
                    new { id });
            }

            var uploadDirectory =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "challenge-proofs");

            Directory.CreateDirectory(
                uploadDirectory);

            var fileName =
                $"{Guid.NewGuid():N}{extension}";

            var filePath =
                Path.Combine(
                    uploadDirectory,
                    fileName);

            try
            {
                await using (
                    var stream =
                        new FileStream(
                            filePath,
                            FileMode.CreateNew))
                {
                    await video.CopyToAsync(stream);
                }

                var mediaInfo =
                    await FFProbe.AnalyseAsync(
                        filePath);

                var duration =
                    mediaInfo.Duration;

                if (duration < MinimumVideoDuration ||
                    duration > MaximumVideoDuration)
                {
                    System.IO.File.Delete(
                        filePath);

                    TempData["ChallengeError"] =
                        $"Your video must be approximately 5 minutes long. The accepted duration is 4:50 to 5:10. Your video was {duration:mm\\:ss}.";

                    return RedirectToAction(
                        "SubmitProof",
                        new { id });
                }

                var videoPath =
                    $"/uploads/challenge-proofs/{fileName}";

                var proof =
                    new ChallengeProof
                    {
                        FitnessChallengeId =
                            challenge.FitnessChallengeId,

                        MemberId =
                            currentMemberId,

                        VideoPath =
                            videoPath,

                        Status =
                            "PendingReview",

                        SubmittedAt =
                            DateTime.Now
                    };

                challenge.Status =
                    "PendingReview";

                challenge.Participation.Status =
                    "PendingReview";

                _context.ChallengeProofs.Add(
                    proof);

                await _context.SaveChangesAsync();

                TempData["ChallengeSuccess"] =
                    "Your 5-minute proof has been submitted successfully. A trainer will review your video.";

                return RedirectToAction(
                    "MyChallenges");
            }
            catch
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                TempData["ChallengeError"] =
                    "The uploaded video could not be processed. Please make sure it is a valid video file and try again.";

                return RedirectToAction(
                    "SubmitProof",
                    new { id });
            }
        }
    }
}