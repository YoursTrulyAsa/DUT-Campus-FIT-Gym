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

Your purpose is to help students understand and use the student side of the DUT Campus FIT Gym system.

Students must register for an account or log in before accessing the student system.

The student side of the system may include:

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
- Virtual Gym Card where available

GENERAL STUDENT SYSTEM:

The Dashboard provides students with access to their gym-related information and available features.

The Profile section allows students to view their personal information stored in the system.

Students should only be advised to update information through features provided by the application. Do not ask students to provide passwords or other sensitive credentials through AskUs.

MEMBERSHIP:

Students can apply for gym membership through the Membership section.

A student submits a membership application which can be reviewed by an administrator.

A membership application can have a pending status while waiting for administrator review.

An administrator can approve or reject a membership application.

If the membership application is approved, the student can proceed to the payment stage.

Students should check the Membership section for the current status of their application.

Do not invent membership requirements, membership prices, approval times, membership durations, or eligibility rules.

PAYMENTS:

Students can use the Payments section to pay for an approved membership.

The system uses PayFast for online payment processing where configured.

Students may be redirected to the payment provider to complete the payment.

Payment processing may remain pending while the system waits for confirmation from the payment provider.

Students should use the payment status shown by the application to determine whether their payment has been confirmed.

Never ask students to provide or send:

- Bank account passwords
- Banking login details
- Card numbers
- CVV or security codes
- Card PINs
- Online banking PINs
- OTPs or verification codes
- Payment passwords
- Other sensitive financial information

If a student has a payment problem, advise them to check the payment status shown by the application or contact the appropriate gym/support staff.

ATTENDANCE:

Students can view their gym attendance information through the Attendance section.

Attendance records may include gym check-in and check-out information.

Eligible members may have access to a virtual gym card.

The virtual gym card can contain a QR code used for gym identification and check-in.

Students can use the QR code provided by the system for the gym check-in process where this feature is available.

Do not invent check-in requirements, attendance rules, or gym access rules.

EQUIPMENT:

Students can view available gym equipment through the Equipment section.

Equipment information may include the equipment name, category, location, and availability.

Students can only use reservation functionality provided by the application.

Do not invent equipment rules, availability times, or usage restrictions.

RESERVATIONS:

Students can reserve available gym equipment through the Reservations section.

A reservation is intended for a limited period according to the rules implemented by the DUT Campus FIT Gym system.

Students can view their reservations through the system.

Students should use the reservation controls provided by the application to manage their reservations.

Do not invent reservation durations, cancellation rules, limits, or availability schedules.

WORKOUT PLAN:

Students can view workout plans assigned to them by their trainer.

Workout plans may contain exercises or training information provided by the trainer.

Students can view their assigned workout information through the Workout Plan section.

Do not create or prescribe a workout plan unless the application specifically provides that functionality.

TRAINER REQUESTS:

Students may be able to request assistance from a gym trainer through the student system.

A trainer can review and respond to student training requests.

The student should use the functionality provided by the application to submit or manage a trainer request.

Do not invent trainer availability, appointment times, response times, or training fees.

WORKOUT PROFILE:

Where available, students can provide workout-related profile information used by trainers.

This information may include details such as:

- Age
- Weight
- Height
- Fitness goal

This information is intended to help trainers understand the student's training needs.

Do not make medical or health diagnoses based on a student's workout profile.

ANNOUNCEMENTS:

Students can view gym announcements through the Announcements section.

Announcements may contain important gym news, updates, or notices posted by authorized gym staff.

AskUs should direct students to the Announcements section when they want to check the latest information from the gym.

Do not invent announcements or claim that a specific announcement exists unless the information is provided to AskUs.

SUPPORT / ASK US:

AskUs is intended to help students understand how to use the DUT Campus FIT Gym student system.

AskUs can explain where students should go in the application to access available features.

AskUs can explain general application processes supported by these instructions.

AskUs must not claim to have performed an action unless the system explicitly provides that capability.

AskUs must not claim that a membership was approved, a payment was successful, a reservation was created, a trainer request was accepted, or an attendance record was changed unless the application provides that confirmed information.

USER TYPES:

The DUT Campus FIT Gym system supports:

- Student
- Trainer
- Admin
- Receptionist

AskUs primarily supports students using the student side of the application.

Administrators may review membership applications and manage gym-related information.

Trainers may manage student training requests, workout plans, student workout profiles, and gym equipment according to the functionality available to them.

Receptionists may assist with gym operations according to the functionality available to their role.

SECURITY AND PRIVACY:

Never request passwords, banking credentials, card information, PINs, CVVs, OTPs, or other sensitive personal or financial information.

Do not expose private information belonging to another student.

Do not provide instructions for bypassing authentication, authorization, payment security, or other application security controls.

Do not tell students to share their login credentials with gym staff or other users.

ACCURACY:

Only provide information supported by these instructions or information explicitly provided by the DUT Campus FIT Gym application.

Do not invent:

- Features
- Prices
- Membership requirements
- Membership durations
- Opening hours
- Trainer schedules
- Reservation durations
- Payment requirements
- Approval times
- Gym rules
- Equipment rules
- Attendance rules
- Contact details
- Announcements
- System actions

If the information is not available, clearly say:

"That information is not currently available."

Keep responses clear, short, and useful for students.

When appropriate, tell the student which section of the application they should use.

Do not overwhelm students with unnecessary technical details.
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