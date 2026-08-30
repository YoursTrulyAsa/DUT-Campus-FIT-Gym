using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace DUT_Campus_FIT_Gym.Controllers
{
    [Authorize]
    public class SupportController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public SupportController(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Index()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                ViewBag.Error = "Please enter a question.";
                return View();
            }

            string? apiKey =
                _configuration["GeminiApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                ViewBag.Error =
                    "The Ask Us service is not configured yet.";

                return View();
            }

            var client =
                _httpClientFactory.CreateClient();

            string url =
                "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash-lite:generateContent";


            string systemInstruction = """
You are AskUs, the support assistant for the DUT Campus FIT Gym application.

Your purpose is to help students understand and use the student side of
the DUT Campus FIT Gym system.

Students must create an account or log in before using the student system.

The student dashboard contains:

- Dashboard
- Profile
- Membership
- Attendance
- Equipment
- Reservations
- Workout Plan
- Payments
- Announcements
- Ask Us / Support

MEMBERSHIP:

Students can apply for gym membership through the Membership section.

A membership application may need administrator approval before payment.

After a membership is approved, the student can proceed with payment.

PAYMENTS:

Students can use the Payment section to make their membership payment.

Never ask a student to provide bank account passwords, card numbers,
CVV numbers, PINs, banking login details, or other sensitive information.

EQUIPMENT:

Students can view gym equipment through the Equipment section.

RESERVATIONS:

Students can reserve available equipment.

A reservation is intended for a limited period according to the rules
implemented by the DUT Campus FIT Gym system.

WORKOUT PLAN:

Students can view workout plans assigned by their trainer.

ATTENDANCE:

Students can view their gym attendance information.

A virtual gym card may be available to eligible members for gym check-in.

ANNOUNCEMENTS:

Students can view gym announcements through the Announcements section.

USER TYPES:

The system supports:

- Student
- Trainer
- Admin
- Receptionist

AskUs primarily supports students using the student side of the system.

IMPORTANT:

Only provide information supported by these instructions.

Do not invent features, prices, rules, procedures, opening times,
membership requirements, or payment requirements.

If you do not know the answer, clearly say that the information is not
currently available.

Keep answers clear, short and useful for students.
""";


            var requestBody = new
            {
                systemInstruction = new
                {
                    parts = new[]
                    {
                        new
                        {
                            text = systemInstruction
                        }
                    }
                },

                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = question
                            }
                        }
                    }
                }
            };


            string json =
                JsonSerializer.Serialize(requestBody);


            var request =
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
                    ViewBag.Question =
                        question;

                    ViewBag.Error =
                        "The Ask Us service could not process your question.";

                    return View();
                }


                using JsonDocument document =
                    JsonDocument.Parse(responseContent);


                string answer =
                    "Sorry, I could not get an answer.";


                if (
                    document.RootElement.TryGetProperty(
                        "candidates",
                        out JsonElement candidates)
                    &&
                    candidates.GetArrayLength() > 0
                )
                {
                    JsonElement candidate =
                        candidates[0];


                    if (
                        candidate.TryGetProperty(
                            "content",
                            out JsonElement content)
                        &&
                        content.TryGetProperty(
                            "parts",
                            out JsonElement parts)
                    )
                    {
                        foreach (
                            JsonElement part
                            in parts.EnumerateArray())
                        {
                            if (
                                part.TryGetProperty(
                                    "text",
                                    out JsonElement text)
                            )
                            {
                                answer =
                                    text.GetString()
                                    ??
                                    "Sorry, I could not get an answer.";

                                break;
                            }
                        }
                    }
                }


                ViewBag.Question =
                    question;

                ViewBag.Answer =
                    answer;


                return View();
            }
            catch
            {
                ViewBag.Question =
                    question;

                ViewBag.Error =
                    "The Ask Us service is currently unavailable.";

                return View();
            }
        }
    }
}