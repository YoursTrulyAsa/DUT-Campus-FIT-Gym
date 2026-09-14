using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace DUT_Campus_FIT_Gym.Controllers
{
    public class BankingController : Controller
    {
        private readonly GymDbContext _context;
        private readonly PayFastSettings _payFast;
        private readonly MembershipPricingService _pricingService;
        private readonly ILogger<BankingController> _logger;

        public BankingController(
            GymDbContext context,
            IOptions<PayFastSettings> payFast,
            MembershipPricingService pricingService,
            ILogger<BankingController> logger)
        {
            _context = context;
            _payFast = payFast.Value;
            _pricingService = pricingService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int membershipId)
        {
            var membership =
                await _context.Memberships
                    .FirstOrDefaultAsync(m =>
                        m.MembershipId == membershipId);

            if (membership == null)
            {
                return NotFound();
            }

            if (membership.Status != "WaitingForPayment")
            {
                return RedirectToAction(
                    "Membership",
                    "Member");
            }

            ViewBag.MembershipId = membershipId;
            ViewBag.Amount = membership.Price;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            BankDetails objBank,
            int membershipId)
        {
            var membership =
                await _context.Memberships
                    .FirstOrDefaultAsync(m =>
                        m.MembershipId == membershipId);

            if (membership == null)
            {
                return NotFound();
            }

            if (membership.Status != "WaitingForPayment")
            {
                return RedirectToAction(
                    "Membership",
                    "Member");
            }

            if (ModelState.IsValid)
            {
                return RedirectToAction(
                    nameof(PayFast),
                    new { membershipId });
            }

            ViewBag.MembershipId = membershipId;
            ViewBag.Amount = membership.Price;

            return View(objBank);
        }

        [HttpGet]
        public async Task<IActionResult> PayFast(int membershipId)
        {
            try
            {
                var membership =
                    await _context.Memberships
                        .Include(m => m.Member)
                        .FirstOrDefaultAsync(m =>
                            m.MembershipId == membershipId);

                if (membership == null)
                {
                    return NotFound();
                }

                if (membership.Status != "WaitingForPayment")
                {
                    return RedirectToAction(
                        "Membership",
                        "Member");
                }

                if (!membership.MemberId.HasValue ||
                    membership.Member == null)
                {
                    TempData["Error"] =
                        "This membership is not linked to a member.";

                    return RedirectToAction(
                        "Membership",
                        "Member");
                }

                var paymentId =
                    Guid.NewGuid().ToString("N");

                membership.PaymentReference = paymentId;
                membership.PaymentStatus = "Pending";

                var payment = new Payment
                {
                    MemberId = membership.MemberId.Value,
                    MembershipId = membership.MembershipId,
                    Amount = membership.Price,
                    PaymentMethod = "PayFast",
                    PaymentStatus = "Pending",
                    ReceiptNumber = paymentId
                };

                _context.Payments.Add(payment);

                await _context.SaveChangesAsync();

                var returnUrl =
                    Url.Action(
                        nameof(PaymentSuccess),
                        "Banking",
                        new { membershipId },
                        Request.Scheme);

                var cancelUrl =
                    Url.Action(
                        nameof(PaymentCancelled),
                        "Banking",
                        new { membershipId },
                        Request.Scheme);

                var notifyUrl =
                    Url.Action(
                        nameof(PaymentNotify),
                        "Banking",
                        null,
                        Request.Scheme);

                if (string.IsNullOrWhiteSpace(returnUrl) ||
                    string.IsNullOrWhiteSpace(cancelUrl) ||
                    string.IsNullOrWhiteSpace(notifyUrl))
                {
                    throw new InvalidOperationException(
                        "Could not generate PayFast callback URLs.");
                }

                var paymentData =
                    new Dictionary<string, string>
                    {
                        ["merchant_id"] =
                            _payFast.MerchantId,

                        ["merchant_key"] =
                            _payFast.MerchantKey,

                        ["return_url"] =
                            returnUrl,

                        ["cancel_url"] =
                            cancelUrl,

                        ["notify_url"] =
                            notifyUrl,

                        ["name_first"] =
                            membership.Member.Name,

                        ["name_last"] =
                            membership.Member.Surname,

                        ["email_address"] =
                            membership.Member.Email,

                        ["m_payment_id"] =
                            paymentId,

                        ["amount"] =
                            membership.Price.ToString(
                                "0.00",
                                CultureInfo.InvariantCulture),

                        ["item_name"] =
                            "DUT Campus FIT Gym Membership"
                    };

                var signature =
                    GenerateSignature(paymentData);

                paymentData["signature"] = signature;

                ViewBag.PaymentUrl =
                    "https://sandbox.payfast.co.za/eng/process";

                ViewBag.PaymentData =
                    paymentData;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error initiating PayFast payment for Membership {MembershipId}",
                    membershipId);

                TempData["Error"] =
                    "An error occurred while processing your payment.";

                return RedirectToAction(
                    "Membership",
                    "Member");
            }
        }

        private string GenerateSignature(
            Dictionary<string, string> data)
        {
            var parameterString =
                new StringBuilder();

            foreach (var item in data)
            {
                if (string.IsNullOrWhiteSpace(item.Value))
                {
                    continue;
                }

                var value =
                    Uri.EscapeDataString(
                        item.Value.Trim())
                    .Replace("%20", "+");

                parameterString.Append(
                    item.Key);

                parameterString.Append("=");

                parameterString.Append(value);

                parameterString.Append("&");
            }

            var signatureString =
                parameterString
                    .ToString()
                    .TrimEnd('&');

            if (!string.IsNullOrWhiteSpace(
                _payFast.Passphrase))
            {
                signatureString +=
                    "&passphrase=" +
                    Uri.EscapeDataString(
                        _payFast.Passphrase.Trim())
                    .Replace("%20", "+");
            }

            using var md5 = MD5.Create();

            var hash =
                md5.ComputeHash(
                    Encoding.UTF8.GetBytes(
                        signatureString));

            return Convert.ToHexString(hash)
                .ToLowerInvariant();
        }

        [HttpGet]
        public async Task<IActionResult> PaymentSuccess(
            int membershipId)
        {
            try
            {
                var membership =
                    await _context.Memberships
                        .FirstOrDefaultAsync(m =>
                            m.MembershipId == membershipId);

                if (membership == null)
                {
                    return NotFound();
                }

                if (membership.Status == "Active")
                {
                    return RedirectToAction(
                        nameof(PaymentComplete),
                        new { membershipId });
                }

                ViewBag.MembershipId =
                    membershipId;

                ViewBag.Message =
                    "Your payment was submitted successfully. We are confirming your payment with PayFast.";

                return View("Processing");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error in PaymentSuccess for Membership {MembershipId}",
                    membershipId);

                return RedirectToAction(
                    "Membership",
                    "Member");
            }
        }

        [HttpGet]
        public IActionResult PaymentCancelled(
            int membershipId)
        {
            TempData["Error"] =
                "Your payment was cancelled. Your membership is still waiting for payment.";

            return RedirectToAction(
                "Membership",
                "Member");
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> PaymentNotify()
        {
            try
            {
                Request.EnableBuffering();

                using var reader =
                    new StreamReader(
                        Request.Body,
                        Encoding.UTF8,
                        leaveOpen: true);

                var rawBody =
                    await reader.ReadToEndAsync();

                Request.Body.Position = 0;

                var form =
                    await Request.ReadFormAsync();

                var receivedSignature =
                    form["signature"]
                        .ToString()
                        .Trim()
                        .ToLowerInvariant();

                var signatureParts =
                    new List<string>();

                foreach (var key in form.Keys)
                {
                    if (key.Equals(
                        "signature",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var value =
                        form[key]
                            .ToString()
                            .Trim();

                    var encodedValue =
                        Uri.EscapeDataString(value)
                            .Replace("%20", "+");

                    signatureParts.Add(
                        $"{key}={encodedValue}");
                }

                var signatureString =
                    string.Join(
                        "&",
                        signatureParts);

                if (!string.IsNullOrWhiteSpace(
                    _payFast.Passphrase))
                {
                    signatureString +=
                        "&passphrase=" +
                        Uri.EscapeDataString(
                            _payFast.Passphrase.Trim())
                        .Replace("%20", "+");
                }

                using var md5 = MD5.Create();

                var calculatedSignature =
                    Convert.ToHexString(
                        md5.ComputeHash(
                            Encoding.UTF8.GetBytes(
                                signatureString)))
                    .ToLowerInvariant();

                if (!string.Equals(
                    receivedSignature,
                    calculatedSignature,
                    StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "Invalid PayFast ITN signature.");

                    return BadRequest(
                        "Invalid signature");
                }

                var merchantId =
                    form["merchant_id"]
                        .ToString();

                if (!string.Equals(
                    merchantId,
                    _payFast.MerchantId,
                    StringComparison.Ordinal))
                {
                    _logger.LogWarning(
                        "Invalid PayFast merchant ID.");

                    return BadRequest(
                        "Invalid merchant");
                }

                var paymentStatus =
                    form["payment_status"]
                        .ToString();

                var mPaymentId =
                    form["m_payment_id"]
                        .ToString();

                var amountGross =
                    form["amount_gross"]
                        .ToString();

                if (string.IsNullOrWhiteSpace(
                    mPaymentId))
                {
                    return BadRequest(
                        "Missing payment ID");
                }

                var payment =
                    await _context.Payments
                        .FirstOrDefaultAsync(p =>
                            p.ReceiptNumber == mPaymentId);

                if (payment == null)
                {
                    _logger.LogWarning(
                        "Payment not found for {PaymentId}",
                        mPaymentId);

                    return Ok();
                }

                var membership =
                    await _context.Memberships
                        .FirstOrDefaultAsync(m =>
                            m.PaymentReference == mPaymentId);

                if (membership == null)
                {
                    _logger.LogWarning(
                        "Membership not found for {PaymentId}",
                        mPaymentId);

                    return Ok();
                }

                if (!decimal.TryParse(
                    amountGross,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var paidAmount))
                {
                    _logger.LogWarning(
                        "Invalid PayFast amount for {PaymentId}",
                        mPaymentId);

                    return BadRequest(
                        "Invalid amount");
                }

                if (Math.Abs(
                    paidAmount - payment.Amount) > 0.01m)
                {
                    _logger.LogWarning(
                        "PayFast amount mismatch for {PaymentId}. Expected {Expected}, Received {Received}",
                        mPaymentId,
                        payment.Amount,
                        paidAmount);

                    return BadRequest(
                        "Amount mismatch");
                }

                if (paymentStatus.Equals(
                    "COMPLETE",
                    StringComparison.OrdinalIgnoreCase))
                {
                    if (membership.Status == "Active" &&
                        payment.PaymentStatus == "Completed")
                    {
                        return Ok();
                    }

                    var dates =
                        _pricingService.GetMembershipDates(
                            membership.MembershipType,
                            DateTime.Today.Year);

                    membership.Status = "Active";
                    membership.PaymentStatus = "Completed";
                    membership.PaymentDate = DateTime.Now;
                    membership.StartDate = dates.StartDate;
                    membership.EndDate = dates.EndDate;

                    payment.PaymentStatus = "Completed";
                    payment.PaymentDate = DateTime.Now;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Membership {MembershipId} activated after successful PayFast payment.",
                        membership.MembershipId);
                }
                else if (
                    paymentStatus.Equals(
                        "CANCELLED",
                        StringComparison.OrdinalIgnoreCase) ||
                    paymentStatus.Equals(
                        "FAILED",
                        StringComparison.OrdinalIgnoreCase))
                {
                    membership.PaymentStatus = "Failed";
                    payment.PaymentStatus = "Failed";

                    await _context.SaveChangesAsync();
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing PayFast ITN.");

                return Ok();
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckPaymentStatus(
            int membershipId)
        {
            var membership =
                await _context.Memberships
                    .FirstOrDefaultAsync(m =>
                        m.MembershipId == membershipId);

            if (membership == null)
            {
                return NotFound();
            }

            return Json(
                new
                {
                    status = membership.PaymentStatus,
                    membershipStatus = membership.Status
                });
        }

        [HttpGet]
        public async Task<IActionResult> PaymentComplete(
            int membershipId)
        {
            try
            {
                var membership =
                    await _context.Memberships
                        .FirstOrDefaultAsync(m =>
                            m.MembershipId == membershipId);

                if (membership == null)
                {
                    return NotFound();
                }

                if (membership.Status != "Active")
                {
                    return RedirectToAction(
                        nameof(PaymentSuccess),
                        new { membershipId });
                }

                ViewBag.MembershipType =
                    membership.MembershipType;

                ViewBag.ExpiryDate =
                    membership.EndDate?
                        .ToString(
                            "MMMM dd, yyyy");

                ViewBag.Amount =
                    membership.Price;

                return View("Tick");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error in PaymentComplete for Membership {MembershipId}",
                    membershipId);

                return RedirectToAction(
                    "Membership",
                    "Member");
            }
        }

        [HttpGet]
        public IActionResult Processing(
            int membershipId)
        {
            ViewBag.MembershipId =
                membershipId;

            ViewBag.RefreshInterval =
                5;

            ViewBag.MaxAttempts =
                24;

            return View(
                "~/Views/Banking/Processing.cshtml");
        }
    }
}