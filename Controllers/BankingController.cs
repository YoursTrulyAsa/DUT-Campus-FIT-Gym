using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace DUT_Campus_FIT_Gym.Controllers
{
    public class BankingController : Controller
    {
        private readonly GymDbContext _context;
        private readonly PayFastSettings _payFastSettings;
        private readonly MembershipPricingService _pricingService;
        private readonly ILogger<BankingController> _logger;
        private readonly NgrokService _ngrokService;

        public BankingController(
            GymDbContext context,
            IOptions<PayFastSettings> payFastSettings,
            MembershipPricingService pricingService,
            ILogger<BankingController> logger,
            NgrokService ngrokService)
        {
            _context = context;
            _payFastSettings = payFastSettings.Value;
            _pricingService = pricingService;
            _logger = logger;
            _ngrokService = ngrokService;
        }

        private int? GetMemberId()
        {
            var claim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(claim))
            {
                return null;
            }

            if (!int.TryParse(
                claim,
                out var memberId))
            {
                return null;
            }

            return memberId;
        }

        [HttpGet]
        public IActionResult Index(int membershipId)
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var membership =
                _context.Memberships
                    .FirstOrDefault(m =>
                        m.MembershipId == membershipId &&
                        m.MemberId == memberId.Value);

            if (membership == null)
            {
                return NotFound();
            }

            if (membership.Status != "WaitingForPayment")
            {
                TempData["PaymentError"] =
                    "This membership is not currently available for payment.";

                return RedirectToAction(
                    "Membership",
                    "Member");
            }

            ViewBag.MembershipId =
                membership.MembershipId;

            ViewBag.Amount =
                membership.Price;

            ViewBag.MembershipType =
                membership.MembershipType;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(
            BankDetails objBank,
            int membershipId)
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var membership =
                _context.Memberships
                    .FirstOrDefault(m =>
                        m.MembershipId == membershipId &&
                        m.MemberId == memberId.Value);

            if (membership == null)
            {
                return NotFound();
            }

            if (membership.Status != "WaitingForPayment")
            {
                TempData["PaymentError"] =
                    "This membership is not available for payment.";

                return RedirectToAction(
                    "Membership",
                    "Member");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.MembershipId =
                    membership.MembershipId;

                ViewBag.Amount =
                    membership.Price;

                ViewBag.MembershipType =
                    membership.MembershipType;

                return View(objBank);
            }

            return RedirectToAction(
                nameof(PayFast),
                new
                {
                    membershipId
                });
        }

        [HttpGet]
        public async Task<IActionResult> PayFast(
            int membershipId)
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var membership =
                await _context.Memberships
                    .Include(m => m.Member)
                    .FirstOrDefaultAsync(m =>
                        m.MembershipId == membershipId &&
                        m.MemberId == memberId.Value);

            if (membership == null)
            {
                return NotFound();
            }

            if (membership.Status != "WaitingForPayment")
            {
                TempData["PaymentError"] =
                    "This membership is not currently available for payment.";

                return RedirectToAction(
                    "Membership",
                    "Member");
            }

            var existingPendingPayment =
                await _context.Payments
                    .FirstOrDefaultAsync(p =>
                        p.MembershipId == membership.MembershipId &&
                        p.PaymentStatus == "Pending");

            string paymentId;

            if (existingPendingPayment != null &&
                !string.IsNullOrWhiteSpace(
                    existingPendingPayment.ReceiptNumber))
            {
                paymentId =
                    existingPendingPayment.ReceiptNumber;

                existingPendingPayment.Amount =
                    membership.Price;

                existingPendingPayment.PaymentMethod =
                    "PayFast";

                existingPendingPayment.PaymentDate =
                    DateTime.Now;
            }
            else
            {
                paymentId =
                    Guid.NewGuid().ToString("N");

                var payment =
                    new Payment
                    {
                        MemberId =
                            membership.MemberId.Value,

                        MembershipId =
                            membership.MembershipId,

                        EquipmentPenaltyId =
                            null,

                        PrivateTrainerSubscriptionId =
                            null,

                        TrainerBookingId =
                            null,

                        Amount =
                            membership.Price,

                        PaymentMethod =
                            "PayFast",

                        PaymentStatus =
                            "Pending",

                        PaymentDate =
                            DateTime.Now,

                        ReceiptNumber =
                            paymentId
                    };

                _context.Payments.Add(payment);
            }

            membership.PaymentReference =
                paymentId;

            membership.PaymentStatus =
                "Pending";

            await _context.SaveChangesAsync();

            var publicUrl =
                await _ngrokService.GetPublicUrlAsync();

            if (string.IsNullOrWhiteSpace(publicUrl))
            {
                var pendingPayment =
                    await _context.Payments
                        .FirstOrDefaultAsync(p =>
                            p.ReceiptNumber == paymentId);

                if (pendingPayment != null)
                {
                    pendingPayment.PaymentStatus =
                        "Failed";
                }

                await _context.SaveChangesAsync();

                TempData["PaymentError"] =
                    "Unable to connect to the payment service.";

                return RedirectToAction(
                    "Membership",
                    "Member");
            }

            publicUrl =
                publicUrl.TrimEnd('/');

            var returnUrl =
                $"{publicUrl}/Banking/PaymentSuccess?membershipId={membershipId}";

            var cancelUrl =
                $"{publicUrl}/Banking/PaymentCancelled?membershipId={membershipId}";

            var notifyUrl =
                $"{publicUrl}/Banking/PaymentNotify";

            var paymentData =
                CreatePaymentData(
                    paymentId,
                    membership.Price,
                    membership.Member?.Name,
                    membership.Member?.Surname,
                    membership.Member?.Email,
                    returnUrl,
                    cancelUrl,
                    notifyUrl,
                    $"DUT Campus FIT Gym {membership.MembershipType} Membership");

            return Redirect(
                BuildPayFastUrl(paymentData));
        }

        [HttpGet]
        public async Task<IActionResult> PayPenalty(
            int penaltyId)
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var penalty =
                await _context.EquipmentPenalties
                    .Include(p => p.Member)
                    .FirstOrDefaultAsync(p =>
                        p.EquipmentPenaltyId == penaltyId &&
                        p.MemberId == memberId.Value);

            if (penalty == null)
            {
                return NotFound();
            }

            if (penalty.Status != "Outstanding")
            {
                TempData["PaymentError"] =
                    "This equipment penalty has already been paid.";

                return RedirectToAction(
                    "Payment",
                    "Member");
            }

            var existingPayment =
                await _context.Payments
                    .FirstOrDefaultAsync(p =>
                        p.EquipmentPenaltyId == penaltyId &&
                        p.PaymentStatus == "Pending");

            string paymentId;

            if (existingPayment != null &&
                !string.IsNullOrWhiteSpace(
                    existingPayment.ReceiptNumber))
            {
                paymentId =
                    existingPayment.ReceiptNumber;

                existingPayment.Amount =
                    penalty.Amount;

                existingPayment.PaymentDate =
                    DateTime.Now;
            }
            else
            {
                paymentId =
                    Guid.NewGuid().ToString("N");

                var payment =
                    new Payment
                    {
                        MemberId =
                            penalty.MemberId,

                        MembershipId =
                            null,

                        EquipmentPenaltyId =
                            penalty.EquipmentPenaltyId,

                        PrivateTrainerSubscriptionId =
                            null,

                        TrainerBookingId =
                            null,

                        Amount =
                            penalty.Amount,

                        PaymentMethod =
                            "PayFast",

                        PaymentStatus =
                            "Pending",

                        PaymentDate =
                            DateTime.Now,

                        ReceiptNumber =
                            paymentId
                    };

                _context.Payments.Add(payment);
            }

            await _context.SaveChangesAsync();

            var publicUrl =
                await _ngrokService.GetPublicUrlAsync();

            if (string.IsNullOrWhiteSpace(publicUrl))
            {
                var pendingPayment =
                    await _context.Payments
                        .FirstOrDefaultAsync(p =>
                            p.ReceiptNumber == paymentId);

                if (pendingPayment != null)
                {
                    pendingPayment.PaymentStatus =
                        "Failed";
                }

                await _context.SaveChangesAsync();

                TempData["PaymentError"] =
                    "Unable to connect to the payment service.";

                return RedirectToAction(
                    "Payment",
                    "Member");
            }

            publicUrl =
                publicUrl.TrimEnd('/');

            var returnUrl =
                $"{publicUrl}/Banking/PaymentSuccessPenalty?penaltyId={penaltyId}";

            var cancelUrl =
                $"{publicUrl}/Banking/PaymentCancelledPenalty?penaltyId={penaltyId}";

            var notifyUrl =
                $"{publicUrl}/Banking/PaymentNotify";

            var paymentData =
                CreatePaymentData(
                    paymentId,
                    penalty.Amount,
                    penalty.Member?.Name,
                    penalty.Member?.Surname,
                    penalty.Member?.Email,
                    returnUrl,
                    cancelUrl,
                    notifyUrl,
                    "DUT Campus FIT Gym Equipment Penalty");

            return Redirect(
                BuildPayFastUrl(paymentData));
        }

        [HttpGet]
        public IActionResult PrivateTrainerPaymentCancelled(
            int subscriptionId)
        {
            TempData["PrivateTrainerError"] =
                "Private trainer payment was cancelled.";

            return RedirectToAction(
                "PrivateTrainer",
                "Member");
        }

        [HttpGet]
        public async Task<IActionResult> PaymentSuccess(
            int membershipId)
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var membership =
                await _context.Memberships
                    .FirstOrDefaultAsync(m =>
                        m.MembershipId == membershipId &&
                        m.MemberId == memberId.Value);

            if (membership == null)
            {
                return NotFound();
            }

            for (var attempt = 0; attempt < 10; attempt++)
            {
                await _context.Entry(membership)
                    .ReloadAsync();

                if (membership.Status == "Active" &&
                    membership.PaymentStatus == "Completed")
                {
                    return View("PaymentComplete");
                }

                await Task.Delay(1000);
            }

            TempData["PaymentSuccess"] =
                "Payment received. We are confirming your payment.";

            return RedirectToAction(
                "Payment",
                "Member");
        }

        [HttpGet]
        public async Task<IActionResult> PaymentSuccessPenalty(
            int penaltyId)
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var penalty =
                await _context.EquipmentPenalties
                    .FirstOrDefaultAsync(p =>
                        p.EquipmentPenaltyId == penaltyId &&
                        p.MemberId == memberId.Value);

            if (penalty == null)
            {
                return NotFound();
            }

            if (penalty.Status == "Paid")
            {
                TempData["PaymentSuccess"] =
                    "Equipment penalty payment successful.";

                return RedirectToAction(
                    "Payment",
                    "Member");
            }

            TempData["PaymentSuccess"] =
                "Penalty payment received. We are confirming your payment.";

            return RedirectToAction(
                "Payment",
                "Member");
        }

        [HttpGet]
        public IActionResult PaymentCancelled(
            int membershipId)
        {
            TempData["PaymentError"] =
                "Payment was cancelled.";

            return RedirectToAction(
                "Membership",
                "Member");
        }

        [HttpGet]
        public IActionResult PaymentCancelledPenalty(
            int penaltyId)
        {
            TempData["PaymentError"] =
                "Equipment penalty payment was cancelled.";

            return RedirectToAction(
                "Payment",
                "Member");
        }

        [HttpGet]
        public async Task<IActionResult> PayPrivateTrainer(
            int subscriptionId)
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var subscription =
                await _context.PrivateTrainerSubscriptions
                    .Include(s => s.Member)
                    .Include(s => s.Trainer)
                    .FirstOrDefaultAsync(s =>
                        s.PrivateTrainerSubscriptionId == subscriptionId &&
                        s.MemberId == memberId.Value);

            if (subscription == null)
            {
                return NotFound();
            }

            if (subscription.Status != "PendingPayment")
            {
                TempData["PrivateTrainerError"] =
                    "This private trainer subscription is not available for payment.";

                return RedirectToAction(
                    "PrivateTrainer",
                    "Member");
            }

            var trainerMemberCount =
                await _context.PrivateTrainerSubscriptions
                    .CountAsync(s =>
                        s.TrainerId == subscription.TrainerId &&
                        s.Status == "Active" &&
                        s.EndDate >= DateTime.Now);

            if (trainerMemberCount >= 2)
            {
                subscription.Status =
                    "Cancelled";

                await _context.SaveChangesAsync();

                TempData["PrivateTrainerError"] =
                    "This trainer is no longer available. Please select another trainer.";

                return RedirectToAction(
                    "PrivateTrainer",
                    "Member");
            }

            const decimal privateTrainerFee =
                145m;

            var existingPayment =
                await _context.Payments
                    .FirstOrDefaultAsync(p =>
                        p.PrivateTrainerSubscriptionId ==
                        subscription.PrivateTrainerSubscriptionId &&
                        p.PaymentStatus == "Pending");

            string paymentId;

            if (existingPayment != null &&
                !string.IsNullOrWhiteSpace(
                    existingPayment.ReceiptNumber))
            {
                paymentId =
                    existingPayment.ReceiptNumber;

                existingPayment.Amount =
                    privateTrainerFee;

                existingPayment.PaymentDate =
                    DateTime.Now;
            }
            else
            {
                paymentId =
                    Guid.NewGuid().ToString("N");

                var payment =
                    new Payment
                    {
                        MemberId =
                            subscription.MemberId,

                        MembershipId =
                            null,

                        EquipmentPenaltyId =
                            null,

                        PrivateTrainerSubscriptionId =
                            subscription.PrivateTrainerSubscriptionId,

                        TrainerBookingId =
                            null,

                        Amount =
                            privateTrainerFee,

                        PaymentMethod =
                            "PayFast",

                        PaymentStatus =
                            "Pending",

                        PaymentDate =
                            DateTime.Now,

                        ReceiptNumber =
                            paymentId
                    };

                _context.Payments.Add(payment);
            }

            subscription.Amount =
                privateTrainerFee;

            subscription.PaymentReference =
                paymentId;

            await _context.SaveChangesAsync();

            var publicUrl =
                await _ngrokService.GetPublicUrlAsync();

            if (string.IsNullOrWhiteSpace(publicUrl))
            {
                var pendingPayment =
                    await _context.Payments
                        .FirstOrDefaultAsync(p =>
                            p.ReceiptNumber == paymentId);

                if (pendingPayment != null)
                {
                    pendingPayment.PaymentStatus =
                        "Failed";
                }

                await _context.SaveChangesAsync();

                TempData["PrivateTrainerError"] =
                    "Unable to connect to the payment service.";

                return RedirectToAction(
                    "PrivateTrainer",
                    "Member");
            }

            publicUrl =
                publicUrl.TrimEnd('/');

            var returnUrl =
                $"{publicUrl}/Banking/PrivateTrainerPaymentSuccess?subscriptionId={subscriptionId}";

            var cancelUrl =
                $"{publicUrl}/Banking/PrivateTrainerPaymentCancelled?subscriptionId={subscriptionId}";

            var notifyUrl =
                $"{publicUrl}/Banking/PaymentNotify";

            var paymentData =
                CreatePaymentData(
                    paymentId,
                    privateTrainerFee,
                    subscription.Member?.Name,
                    subscription.Member?.Surname,
                    subscription.Member?.Email,
                    returnUrl,
                    cancelUrl,
                    notifyUrl,
                    "DUT Campus FIT Gym Private Trainer - Monthly");

            return Redirect(
                BuildPayFastUrl(paymentData));
        }

        [HttpGet]
        public async Task<IActionResult> PrivateTrainerPaymentSuccess(
            int subscriptionId)
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var subscription =
                await _context.PrivateTrainerSubscriptions
                    .FirstOrDefaultAsync(s =>
                        s.PrivateTrainerSubscriptionId == subscriptionId &&
                        s.MemberId == memberId.Value);

            if (subscription == null)
            {
                return NotFound();
            }

            if (subscription.Status == "Active")
            {
                TempData["PrivateTrainerSuccess"] =
                    "Your private trainer payment was successful. Your trainer is now active.";

                return RedirectToAction(
                    "PrivateTrainer",
                    "Member");
            }

            TempData["PrivateTrainerSuccess"] =
                "Payment received. We are confirming your payment.";

            return RedirectToAction(
                "PrivateTrainer",
                "Member");
        }

        [HttpGet]
        public async Task<IActionResult> PayTrainerBooking(
            int bookingId)
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var booking =
                await _context.TrainerBookings
                    .Include(b => b.Trainer)
                    .Include(b => b.Student)
                    .Include(b => b.TrainerRequest)
                    .FirstOrDefaultAsync(b =>
                        b.TrainerBookingId == bookingId &&
                        b.StudentId == memberId.Value);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.Status != "PendingPayment")
            {
                TempData["TrainerRequestError"] =
                    "This trainer session is not available for payment.";

                return RedirectToAction(
                    "MyTrainerRequests",
                    "Member");
            }

            const decimal trainerSessionFee =
                50m;

            var existingPayment =
                await _context.Payments
                    .FirstOrDefaultAsync(p =>
                        p.TrainerBookingId == bookingId &&
                        p.PaymentStatus == "Pending");

            string paymentId;

            if (existingPayment != null &&
                !string.IsNullOrWhiteSpace(
                    existingPayment.ReceiptNumber))
            {
                paymentId =
                    existingPayment.ReceiptNumber;

                existingPayment.Amount =
                    trainerSessionFee;

                existingPayment.PaymentMethod =
                    "PayFast";

                existingPayment.PaymentDate =
                    DateTime.Now;
            }
            else
            {
                paymentId =
                    Guid.NewGuid().ToString("N");

                var payment =
                    new Payment
                    {
                        MemberId =
                            booking.StudentId,

                        MembershipId =
                            null,

                        EquipmentPenaltyId =
                            null,

                        PrivateTrainerSubscriptionId =
                            null,

                        TrainerBookingId =
                            booking.TrainerBookingId,

                        Amount =
                            trainerSessionFee,

                        PaymentMethod =
                            "PayFast",

                        PaymentStatus =
                            "Pending",

                        PaymentDate =
                            DateTime.Now,

                        ReceiptNumber =
                            paymentId
                    };

                _context.Payments.Add(payment);
            }

            await _context.SaveChangesAsync();

            var publicUrl =
                await _ngrokService.GetPublicUrlAsync();

            if (string.IsNullOrWhiteSpace(publicUrl))
            {
                var pendingPayment =
                    await _context.Payments
                        .FirstOrDefaultAsync(p =>
                            p.ReceiptNumber == paymentId);

                if (pendingPayment != null)
                {
                    pendingPayment.PaymentStatus =
                        "Failed";
                }

                booking.Status =
                    "PaymentFailed";

                await _context.SaveChangesAsync();

                TempData["TrainerRequestError"] =
                    "Unable to connect to the payment service.";

                return RedirectToAction(
                    "MyTrainerRequests",
                    "Member");
            }

            publicUrl =
                publicUrl.TrimEnd('/');

            var returnUrl =
                $"{publicUrl}/Banking/PaymentSuccessTrainerBooking?bookingId={bookingId}";

            var cancelUrl =
                $"{publicUrl}/Banking/PaymentCancelledTrainerBooking?bookingId={bookingId}";

            var notifyUrl =
                $"{publicUrl}/Banking/PaymentNotify";

            var paymentData =
                CreatePaymentData(
                    paymentId,
                    trainerSessionFee,
                    booking.Student?.Name,
                    booking.Student?.Surname,
                    booking.Student?.Email,
                    returnUrl,
                    cancelUrl,
                    notifyUrl,
                    "DUT Campus FIT Gym Trainer Session");

            return Redirect(
                BuildPayFastUrl(paymentData));
        }

        [HttpGet]
        public async Task<IActionResult> PaymentSuccessTrainerBooking(
            int bookingId)
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var booking =
                await _context.TrainerBookings
                    .Include(b => b.Trainer)
                    .FirstOrDefaultAsync(b =>
                        b.TrainerBookingId == bookingId &&
                        b.StudentId == memberId.Value);

            if (booking == null)
            {
                return NotFound();
            }

            for (var attempt = 0; attempt < 10; attempt++)
            {
                await _context.Entry(booking)
                    .ReloadAsync();

                if (booking.Status == "Booked")
                {
                    ViewBag.BookingId =
                        booking.TrainerBookingId;

                    ViewBag.Amount =
                        50m;

                    ViewBag.TrainerName =
                        booking.Trainer?.TrainerName ?? "Trainer";

                    ViewBag.SessionDate =
                        booking.StartTime.ToString(
                            "dd MMMM yyyy");

                    ViewBag.StartTime =
                        booking.StartTime.ToString(
                            "HH:mm");

                    ViewBag.EndTime =
                        booking.EndTime.ToString(
                            "HH:mm");

                    return View("PaymentSuccessTrainerBooking");
                }

                if (booking.Status == "PaymentFailed" ||
                    booking.Status == "PaymentCancelled")
                {
                    TempData["TrainerRequestError"] =
                        "Your trainer session payment could not be confirmed.";

                    return RedirectToAction(
                        "MyTrainerRequests",
                        "Member");
                }

                await Task.Delay(1000);
            }

            ViewBag.BookingId =
                booking.TrainerBookingId;

            return View("PaymentProcessingTrainerBooking");
        }

        [HttpGet]
        public async Task<IActionResult> PaymentCancelledTrainerBooking(
            int bookingId)
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var booking =
                await _context.TrainerBookings
                    .FirstOrDefaultAsync(b =>
                        b.TrainerBookingId == bookingId &&
                        b.StudentId == memberId.Value);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.Status == "PendingPayment")
            {
                booking.Status =
                    "PaymentCancelled";

                var payment =
                    await _context.Payments
                        .FirstOrDefaultAsync(p =>
                            p.TrainerBookingId == bookingId &&
                            p.PaymentStatus == "Pending");

                if (payment != null)
                {
                    payment.PaymentStatus =
                        "Failed";
                }

                await _context.SaveChangesAsync();
            }

            TempData["TrainerRequestError"] =
                "Trainer session payment was cancelled.";

            return RedirectToAction(
                "MyTrainerRequests",
                "Member");
        }

        [HttpGet]
        public async Task<IActionResult> CheckTrainerBookingPaymentStatus(
            int bookingId)
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return Json(new
                {
                    success = false,
                    status = "Unauthorised"
                });
            }

            var booking =
                await _context.TrainerBookings
                    .FirstOrDefaultAsync(b =>
                        b.TrainerBookingId == bookingId &&
                        b.StudentId == memberId.Value);

            if (booking == null)
            {
                return Json(new
                {
                    success = false,
                    status = "NotFound"
                });
            }

            var payment =
                await _context.Payments
                    .FirstOrDefaultAsync(p =>
                        p.TrainerBookingId == bookingId);

            var status =
                booking.Status switch
                {
                    "Booked" => "Completed",
                    "PaymentFailed" => "Failed",
                    "PaymentCancelled" => "Cancelled",
                    _ => payment?.PaymentStatus ?? "Pending"
                };

            return Json(new
            {
                success = true,
                status,
                bookingStatus = booking.Status
            });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> PaymentNotify()
        {
            try
            {
                _logger.LogInformation(
                    "PAYFAST ITN RECEIVED. Method: {Method}, Path: {Path}",
                    Request.Method,
                    Request.Path);

                var form =
                    await Request.ReadFormAsync();

                _logger.LogInformation(
                    "PAYFAST ITN FORM: {FormData}",
                    string.Join(
                        " | ",
                        form.Keys.Select(key =>
                            $"{key}={form[key]}")));

                if (!form.ContainsKey("signature"))
                {
                    _logger.LogWarning(
                        "PayFast notification did not contain a signature.");

                    return BadRequest();
                }

                var merchantId =
                    form["merchant_id"].ToString().Trim();

                if (string.IsNullOrWhiteSpace(merchantId) ||
                    merchantId !=
                    _payFastSettings.MerchantId.Trim())
                {
                    _logger.LogWarning(
                        "PayFast merchant ID validation failed.");

                    return BadRequest();
                }

                if (!form.ContainsKey("payment_status") ||
                    !form.ContainsKey("m_payment_id") ||
                    !form.ContainsKey("amount_gross"))
                {
                    return BadRequest();
                }

                var paymentStatus =
                    form["payment_status"]
                        .ToString()
                        .Trim()
                        .ToUpperInvariant();

                var mPaymentId =
                    form["m_payment_id"]
                        .ToString()
                        .Trim();

                var amountGross =
                    form["amount_gross"]
                        .ToString()
                        .Trim();

                var payment =
                    await _context.Payments
                        .Include(p => p.Membership)
                        .Include(p => p.EquipmentPenalty)
                        .Include(p => p.PrivateTrainerSubscription)
                        .Include(p => p.TrainerBooking)
                            .ThenInclude(b => b!.Trainer)
                        .Include(p => p.TrainerBooking)
                            .ThenInclude(b => b!.Student)
                        .FirstOrDefaultAsync(p =>
                            p.ReceiptNumber == mPaymentId);

                if (payment == null)
                {
                    _logger.LogWarning(
                        "PayFast payment {PaymentId} was not found.",
                        mPaymentId);

                    return Ok();
                }

                if (!decimal.TryParse(
                    amountGross,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var paidAmount))
                {
                    _logger.LogWarning(
                        "Invalid PayFast amount received for {PaymentId}: {Amount}.",
                        mPaymentId,
                        amountGross);

                    return BadRequest();
                }

                paidAmount =
                    decimal.Round(
                        paidAmount,
                        2);

                var expectedAmount =
                    decimal.Round(
                        payment.Amount,
                        2);

                if (paidAmount != expectedAmount)
                {
                    _logger.LogWarning(
                        "PayFast payment amount mismatch for {PaymentId}. Expected {ExpectedAmount}, received {ReceivedAmount}.",
                        mPaymentId,
                        expectedAmount,
                        paidAmount);

                    return BadRequest();
                }

                if (payment.EquipmentPenaltyId.HasValue)
                {
                    return await ProcessPenaltyPayment(
                        payment,
                        paymentStatus);
                }

                if (payment.PrivateTrainerSubscriptionId.HasValue)
                {
                    return await ProcessPrivateTrainerPayment(
                        payment,
                        paymentStatus);
                }

                if (payment.TrainerBookingId.HasValue)
                {
                    return await ProcessTrainerBookingPayment(
                        payment,
                        paymentStatus);
                }

                if (payment.MembershipId.HasValue)
                {
                    return await ProcessMembershipPayment(
                        payment,
                        paymentStatus);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing PayFast notification.");

                return BadRequest();
            }
        }

        private async Task<IActionResult> ProcessPenaltyPayment(
            Payment payment,
            string paymentStatus)
        {
            var penalty =
                payment.EquipmentPenalty;

            if (penalty == null)
            {
                return Ok();
            }

            if (paymentStatus == "COMPLETE")
            {
                penalty.Status =
                    "Paid";

                payment.PaymentStatus =
                    "Completed";

                payment.PaymentDate =
                    DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok();
            }

            if (paymentStatus == "CANCELLED" ||
                paymentStatus == "FAILED")
            {
                payment.PaymentStatus =
                    "Failed";

                await _context.SaveChangesAsync();

                return Ok();
            }

            return Ok();
        }

        private async Task<IActionResult> ProcessPrivateTrainerPayment(
            Payment payment,
            string paymentStatus)
        {
            var subscription =
                payment.PrivateTrainerSubscription;

            if (subscription == null)
            {
                return Ok();
            }

            if (paymentStatus == "COMPLETE")
            {
                if (payment.PaymentStatus == "Completed" &&
                    subscription.Status == "Active")
                {
                    return Ok();
                }

                var trainerMemberCount =
                    await _context.PrivateTrainerSubscriptions
                        .CountAsync(s =>
                            s.TrainerId ==
                            subscription.TrainerId &&
                            s.Status == "Active" &&
                            s.EndDate >= DateTime.Now &&
                            s.PrivateTrainerSubscriptionId !=
                            subscription.PrivateTrainerSubscriptionId);

                if (trainerMemberCount >= 2 &&
                    subscription.Status != "Active")
                {
                    subscription.Status =
                        "TrainerFullPaid";

                    payment.PaymentStatus =
                        "Completed";

                    payment.PaymentDate =
                        DateTime.Now;

                    await _context.SaveChangesAsync();

                    return Ok();
                }

                var startDate =
                    DateTime.Now;

                subscription.Status =
                    "Active";

                subscription.StartDate =
                    startDate;

                subscription.EndDate =
                    startDate
                        .AddMonths(1)
                        .AddDays(-1);

                subscription.Amount =
                    145m;

                subscription.PaymentReference =
                    payment.ReceiptNumber;

                payment.PaymentStatus =
                    "Completed";

                payment.PaymentDate =
                    startDate;

                await _context.SaveChangesAsync();

                return Ok();
            }

            if (paymentStatus == "CANCELLED" ||
                paymentStatus == "FAILED")
            {
                subscription.Status =
                    "PaymentFailed";

                payment.PaymentStatus =
                    "Failed";

                await _context.SaveChangesAsync();

                return Ok();
            }

            return Ok();
        }

        private async Task<IActionResult> ProcessTrainerBookingPayment(
            Payment payment,
            string paymentStatus)
        {
            var booking =
                payment.TrainerBooking;

            if (booking == null)
            {
                return Ok();
            }

            if (paymentStatus == "COMPLETE")
            {
                if (payment.PaymentStatus == "Completed" &&
                    booking.Status == "Booked")
                {
                    return Ok();
                }

                if (booking.Status != "PendingPayment")
                {
                    return Ok();
                }

                var trainerConflict =
                    await _context.TrainerBookings
                        .AnyAsync(b =>
                            b.TrainerId == booking.TrainerId &&
                            b.TrainerBookingId != booking.TrainerBookingId &&
                            (
                                b.Status == "Booked" ||
                                b.Status == "PendingPayment"
                            ) &&
                            booking.StartTime < b.EndTime &&
                            booking.EndTime > b.StartTime);

                if (trainerConflict)
                {
                    booking.Status =
                        "PaymentFailed";

                    payment.PaymentStatus =
                        "Failed";

                    await _context.SaveChangesAsync();

                    _logger.LogWarning(
                        "Trainer booking {BookingId} could not be confirmed because of a trainer conflict.",
                        booking.TrainerBookingId);

                    return Ok();
                }

                var studentConflict =
                    await _context.TrainerBookings
                        .AnyAsync(b =>
                            b.StudentId == booking.StudentId &&
                            b.TrainerBookingId != booking.TrainerBookingId &&
                            (
                                b.Status == "Booked" ||
                                b.Status == "PendingPayment"
                            ) &&
                            booking.StartTime < b.EndTime &&
                            booking.EndTime > b.StartTime);

                if (studentConflict)
                {
                    booking.Status =
                        "PaymentFailed";

                    payment.PaymentStatus =
                        "Failed";

                    await _context.SaveChangesAsync();

                    _logger.LogWarning(
                        "Trainer booking {BookingId} could not be confirmed because of a student conflict.",
                        booking.TrainerBookingId);

                    return Ok();
                }

                var dayStart =
                    booking.StartTime.Date;

                var dayEnd =
                    dayStart.AddDays(1);

                var dailySessionCount =
                    await _context.TrainerBookings
                        .CountAsync(b =>
                            b.TrainerId == booking.TrainerId &&
                            b.TrainerBookingId != booking.TrainerBookingId &&
                            (
                                b.Status == "Booked" ||
                                b.Status == "PendingPayment"
                            ) &&
                            b.StartTime >= dayStart &&
                            b.StartTime < dayEnd);

                if (dailySessionCount >= 3)
                {
                    booking.Status =
                        "PaymentFailed";

                    payment.PaymentStatus =
                        "Failed";

                    await _context.SaveChangesAsync();

                    _logger.LogWarning(
                        "Trainer booking {BookingId} could not be confirmed because the trainer already has 3 sessions that day.",
                        booking.TrainerBookingId);

                    return Ok();
                }

                booking.Status =
                    "Booked";

                payment.PaymentStatus =
                    "Completed";

                payment.PaymentDate =
                    DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Trainer booking {BookingId} payment confirmed successfully.",
                    booking.TrainerBookingId);

                return Ok();
            }

            if (paymentStatus == "CANCELLED" ||
                paymentStatus == "FAILED")
            {
                booking.Status =
                    "PaymentFailed";

                payment.PaymentStatus =
                    "Failed";

                await _context.SaveChangesAsync();

                return Ok();
            }

            return Ok();
        }

        private async Task<IActionResult> ProcessMembershipPayment(
            Payment payment,
            string paymentStatus)
        {
            var membership =
                payment.Membership;

            if (membership == null)
            {
                return Ok();
            }

            if (paymentStatus == "COMPLETE")
            {
                if (payment.PaymentStatus == "Completed" &&
                    membership.Status == "Active")
                {
                    return Ok();
                }

                var startDate =
                    DateTime.Now;

                membership.Status =
                    "Active";

                membership.PaymentStatus =
                    "Completed";

                membership.PaymentDate =
                    startDate;

                membership.StartDate =
                    startDate;

                if (membership.MembershipType ==
                    "Semester")
                {
                    membership.EndDate =
                        startDate.AddMonths(6);
                }
                else
                {
                    membership.EndDate =
                        startDate.AddYears(1);
                }

                payment.PaymentStatus =
                    "Completed";

                payment.PaymentDate =
                    startDate;

                await _context.SaveChangesAsync();

                return Ok();
            }

            if (paymentStatus == "CANCELLED" ||
                paymentStatus == "FAILED")
            {
                membership.PaymentStatus =
                    "Failed";

                payment.PaymentStatus =
                    "Failed";

                await _context.SaveChangesAsync();

                return Ok();
            }

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> CheckPaymentStatus(
            int membershipId)
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return Json(new
                {
                    success = false
                });
            }

            var membership =
                await _context.Memberships
                    .FirstOrDefaultAsync(m =>
                        m.MembershipId == membershipId &&
                        m.MemberId == memberId.Value);

            if (membership == null)
            {
                return Json(new
                {
                    success = false
                });
            }

            return Json(new
            {
                success = true,
                status = membership.PaymentStatus,
                membershipStatus = membership.Status
            });
        }

        private List<KeyValuePair<string, string>> CreatePaymentData(
            string paymentId,
            decimal amount,
            string? firstName,
            string? lastName,
            string? email,
            string returnUrl,
            string cancelUrl,
            string notifyUrl,
            string itemName)
        {
            return new List<KeyValuePair<string, string>>
            {
                new(
                    "merchant_id",
                    _payFastSettings.MerchantId),

                new(
                    "merchant_key",
                    _payFastSettings.MerchantKey),

                new(
                    "return_url",
                    returnUrl),

                new(
                    "cancel_url",
                    cancelUrl),

                new(
                    "notify_url",
                    notifyUrl),

                new(
                    "name_first",
                    firstName ?? ""),

                new(
                    "name_last",
                    lastName ?? ""),

                new(
                    "email_address",
                    email ?? ""),

                new(
                    "m_payment_id",
                    paymentId),

                new(
                    "amount",
                    amount.ToString(
                        "0.00",
                        CultureInfo.InvariantCulture)),

                new(
                    "item_name",
                    itemName)
            };
        }

        private List<KeyValuePair<string, string>> BuildNotificationData(
            IFormCollection form)
        {
            var data =
                new List<KeyValuePair<string, string>>();

            foreach (var key in form.Keys)
            {
                if (key.Equals(
                    "signature",
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value =
                    form[key].ToString();

                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                data.Add(
                    new KeyValuePair<string, string>(
                        key.Trim(),
                        value.Trim()));
            }

            return data;
        }

        private string GenerateSignature(
            IEnumerable<KeyValuePair<string, string>> data)
        {
            var parameters =
                new List<string>();

            foreach (var item in data)
            {
                if (string.IsNullOrWhiteSpace(item.Value))
                {
                    continue;
                }

                var key =
                    item.Key.Trim();

                var value =
                    item.Value.Trim();

                parameters.Add(
                    $"{key}={WebUtility.UrlEncode(value)}");
            }

            if (!string.IsNullOrWhiteSpace(
                _payFastSettings.Passphrase))
            {
                parameters.Add(
                    $"passphrase={WebUtility.UrlEncode(
                        _payFastSettings.Passphrase.Trim())}");
            }

            var parameterString =
                string.Join(
                    "&",
                    parameters);

            using var md5 =
                MD5.Create();

            var hash =
                md5.ComputeHash(
                    Encoding.UTF8.GetBytes(
                        parameterString));

            return Convert.ToHexString(hash)
                .ToLowerInvariant();
        }

        private string BuildPayFastUrl(
            IEnumerable<KeyValuePair<string, string>> data)
        {
            var paymentData =
                data
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.Value))
                    .Select(x =>
                        new KeyValuePair<string, string>(
                            x.Key.Trim(),
                            x.Value.Trim()))
                    .ToList();

            var signature =
                GenerateSignature(paymentData);

            var query =
                new StringBuilder();

            foreach (var item in paymentData)
            {
                if (query.Length > 0)
                {
                    query.Append('&');
                }

                query.Append(
                    WebUtility.UrlEncode(
                        item.Key));

                query.Append('=');

                query.Append(
                    WebUtility.UrlEncode(
                        item.Value));
            }

            query.Append("&signature=");

            query.Append(
                WebUtility.UrlEncode(
                    signature));

            return
                $"https://sandbox.payfast.co.za/eng/process?{query}";
        }
    }
}