using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
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
        private readonly ILogger<BankingController> _logger;

        public BankingController(
            GymDbContext context,
            IOptions<PayFastSettings> payFast,
            ILogger<BankingController> logger)
        {
            _context = context;
            _payFast = payFast.Value;
            _logger = logger;
        }

        // =========================================================
        // BANKING DETAILS - GET
        // =========================================================

        [HttpGet]
        public IActionResult Index(int membershipId)
        {
            var membership = _context.Memberships
                .FirstOrDefault(m =>
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

        // =========================================================
        // BANKING DETAILS - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(
            BankDetails objBank,
            int membershipId)
        {
            var membership = _context.Memberships
                .FirstOrDefault(m =>
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
                    "PayFast",
                    new { membershipId });
            }

            ViewBag.MembershipId = membershipId;
            ViewBag.Amount = membership.Price;

            return View(objBank);
        }

        // =========================================================
        // PAYFAST - GET
        // =========================================================

        [HttpGet]
        public IActionResult PayFast(int membershipId)
        {
            try
            {
                var membership = _context.Memberships
                    .Include(m => m.Member)
                    .FirstOrDefault(m =>
                        m.MembershipId == membershipId);

                if (membership == null)
                {
                    _logger.LogWarning(
                        "Membership not found: {MembershipId}",
                        membershipId);

                    return NotFound();
                }

                // =====================================================
                // ONLY WAITING-FOR-PAYMENT MEMBERSHIPS CAN PAY
                // =====================================================

                if (membership.Status != "WaitingForPayment")
                {
                    _logger.LogWarning(
                        "Invalid membership status: {Status} for ID: {MembershipId}",
                        membership.Status,
                        membershipId);

                    return RedirectToAction(
                        "Membership",
                        "Member");
                }

                // =====================================================
                // MEMBER ID MUST EXIST
                // =====================================================

                if (!membership.MemberId.HasValue)
                {
                    _logger.LogError(
                        "Membership {MembershipId} has no MemberId.",
                        membership.MembershipId);

                    TempData["Error"] =
                        "This membership is not linked to a member.";

                    return RedirectToAction(
                        "Membership",
                        "Member");
                }

                // =====================================================
                // GENERATE UNIQUE PAYMENT ID
                // =====================================================

                var paymentId =
                    Guid.NewGuid().ToString();

                // =====================================================
                // SAVE PAYMENT REFERENCE
                // =====================================================

                membership.PaymentReference =
                    paymentId;

                membership.PaymentStatus =
                    "Pending";

                membership.PaymentDate =
                    DateTime.Now;

                // =====================================================
                // CREATE PAYMENT HISTORY RECORD
                // =====================================================

                var payment = new Payment
                {
                    MemberId =
                        membership.MemberId.Value,

                    MembershipId =
                        membership.MembershipId,

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

                _context.SaveChanges();

                // =====================================================
                // BUILD PAYFAST DATA
                // =====================================================

                var paymentData =
                    new Dictionary<string, string>
                    {
                        ["merchant_id"] =
                            _payFast.MerchantId,

                        ["merchant_key"] =
                            _payFast.MerchantKey,

                        ["return_url"] =
                            "https://unguided-handful-comma.ngrok-free.dev/Banking/PaymentSuccess?membershipId="
                            + membershipId,

                        ["cancel_url"] =
                            "https://unguided-handful-comma.ngrok-free.dev/Banking/PaymentCancelled?membershipId="
                            + membershipId,

                        ["notify_url"] =
                            "https://unguided-handful-comma.ngrok-free.dev/Banking/PaymentNotify",

                        ["name_first"] =
                            membership.Member?.Name ?? "DUT",

                        ["name_last"] =
                            membership.Member?.Surname ?? "Student",

                        ["email_address"] =
                            membership.Member?.Email ?? "test@test.com",

                        ["m_payment_id"] =
                            paymentId,

                        ["amount"] =
                            membership.Price.ToString(
                                "0.00",
                                CultureInfo.InvariantCulture),

                        ["item_name"] =
                            "DUT Campus FIT Gym Membership"
                    };

                // =====================================================
                // GENERATE CHECKOUT SIGNATURE
                // =====================================================

                var signature =
                    GenerateSignature(paymentData);

                paymentData["signature"] =
                    signature;

                _logger.LogInformation(
                    "PayFast payment initiated for Membership: {MembershipId}, Payment ID: {PaymentId}",
                    membershipId,
                    paymentId);

                // =====================================================
                // PAYFAST SANDBOX
                // =====================================================

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
                    "Error in PayFast for Membership: {MembershipId}",
                    membershipId);

                TempData["Error"] =
                    "An error occurred while processing your payment.";

                return RedirectToAction(
                    "Membership",
                    "Member");
            }
        }

        // =========================================================
        // CHECKOUT SIGNATURE
        // =========================================================

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
                    item.Value.Trim();

                value =
                    Uri.EscapeDataString(value)
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

            // =====================================================
            // ADD PASSPHRASE
            // =====================================================

            if (!string.IsNullOrWhiteSpace(
                _payFast.Passphrase))
            {
                signatureString +=
                    "&passphrase=" +
                    Uri.EscapeDataString(
                        _payFast.Passphrase.Trim())
                    .Replace("%20", "+");
            }

            _logger.LogInformation(
                "PAYFAST CHECKOUT SIGNATURE STRING: {SignatureString}",
                signatureString);

            // =====================================================
            // MD5
            // =====================================================

            using var md5 =
                MD5.Create();

            var hash =
                md5.ComputeHash(
                    Encoding.UTF8.GetBytes(
                        signatureString));

            return Convert.ToHexString(hash)
                .ToLowerInvariant();
        }

        // =========================================================
        // GET MEMBERSHIP DURATION
        // =========================================================

        private int GetMembershipDurationMonths(
            string? membershipType)
        {
            return membershipType?
                .Trim()
                .ToLowerInvariant()
                switch
            {
                "semester" => 6,

                "annual" => 12,

                "monthly" => 1,

                "quarterly" => 3,

                "half_yearly" => 6,

                "half-yearly" => 6,

                "half yearly" => 6,

                "annually" => 12,

                _ => 1
            };
        }

        // =========================================================
        // PAYMENT SUCCESS / PAYFAST RETURN URL
        // =========================================================

        [HttpGet]
        public IActionResult PaymentSuccess(
            int membershipId)
        {
            try
            {
                var membership =
                    _context.Memberships
                        .FirstOrDefault(
                            m =>
                                m.MembershipId ==
                                membershipId);

                if (membership == null)
                {
                    _logger.LogWarning(
                        "PaymentSuccess: Membership not found: {MembershipId}",
                        membershipId);

                    return NotFound();
                }

                // =====================================================
                // ITN HAS ALREADY ACTIVATED MEMBERSHIP
                // =====================================================

                if (membership.Status == "Active")
                {
                    return RedirectToAction(
                        "PaymentComplete",
                        new
                        {
                            membershipId
                        });
                }

                // =====================================================
                // ITN MAY STILL BE PROCESSING
                // =====================================================

                ViewBag.MembershipId =
                    membershipId;

                ViewBag.Message =
                    "Your payment was successful. We are confirming your payment with PayFast.";

                return View("Processing");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error in PaymentSuccess for Membership: {MembershipId}",
                    membershipId);

                return RedirectToAction(
                    "Membership",
                    "Member");
            }
        }

        // 
        [HttpPost]
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

                _logger.LogInformation(
                    "PAYFAST RAW ITN BODY: {RawBody}",
                    rawBody);

                var form =
                    await Request.ReadFormAsync();

                var receivedSignature =
                    form["signature"]
                        .ToString()
                        .Trim()
                        .ToLowerInvariant();

                _logger.LogInformation(
                    "RECEIVED ITN SIGNATURE: {Signature}",
                    receivedSignature);

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
                        form[key].ToString();

                    var encodedValue =
                        Uri.EscapeDataString(
                            value.Trim())
                            .Replace(
                                "%20",
                                "+");

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
                    var encodedPassphrase =
                        Uri.EscapeDataString(
                            _payFast.Passphrase.Trim())
                        .Replace(
                            "%20",
                            "+");

                    signatureString +=
                        $"&passphrase={encodedPassphrase}";
                }

                _logger.LogInformation(
                    "ITN SIGNATURE STRING: {SignatureString}",
                    signatureString);

                using var md5 =
                    MD5.Create();

                var calculatedSignature =
                    Convert.ToHexString(
                        md5.ComputeHash(
                            Encoding.UTF8.GetBytes(
                                signatureString)))
                    .ToLowerInvariant();

                _logger.LogInformation(
                    "CALCULATED ITN SIGNATURE: {Signature}",
                    calculatedSignature);

                if (!string.Equals(
                    receivedSignature,
                    calculatedSignature,
                    StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "INVALID PAYFAST ITN SIGNATURE");

                    return BadRequest(
                        "Invalid signature");
                }

                _logger.LogInformation(
                    "PAYFAST ITN SIGNATURE VERIFIED SUCCESSFULLY");

                
                var paymentStatus =
                    form["payment_status"]
                        .ToString();

                var mPaymentId =
                    form["m_payment_id"]
                        .ToString();

                var amount =
                    form["amount_gross"]
                        .ToString();

                _logger.LogInformation(
                    "Payment Status: {Status}",
                    paymentStatus);

                _logger.LogInformation(
                    "Payment ID: {PaymentId}",
                    mPaymentId);

                _logger.LogInformation(
                    "Amount: {Amount}",
                    amount);

                var payment =
                    _context.Payments
                        .FirstOrDefault(
                            p =>
                                p.ReceiptNumber ==
                                mPaymentId);

                if (payment == null)
                {
                    _logger.LogWarning(
                        "Payment not found for Payment ID: {PaymentId}",
                        mPaymentId);

                    return Ok();
                }

                var membership =
                    _context.Memberships
                        .FirstOrDefault(
                            m =>
                                m.PaymentReference ==
                                mPaymentId);

                if (membership == null)
                {
                    _logger.LogWarning(
                        "Membership not found for Payment ID: {PaymentId}",
                        mPaymentId);

                    return Ok();
                }

                if (paymentStatus.Equals(
                    "COMPLETE",
                    StringComparison.OrdinalIgnoreCase))
                {
                    if (membership.Status == "Active" &&
                        membership.PaymentStatus == "Completed")
                    {
                        _logger.LogInformation(
                            "Membership already activated: {MembershipId}",
                            membership.MembershipId);

                        return Ok();
                    }

                    var durationMonths =
                        GetMembershipDurationMonths(
                            membership.MembershipType);

                    var startDate =
                        DateTime.Now;

                    var endDate =
                        startDate.AddMonths(
                            durationMonths);

                    membership.Status =
                        "Active";

                    membership.PaymentStatus =
                        "Completed";

                    membership.PaymentDate =
                        DateTime.Now;

                    membership.StartDate =
                        startDate;

                    membership.EndDate =
                        endDate;

                
                    payment.PaymentStatus =
                        "Completed";

                    payment.PaymentDate =
                        DateTime.Now;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "========================================");

                    _logger.LogInformation(
                        "PAYMENT SUCCESSFUL");

                    _logger.LogInformation(
                        "MEMBERSHIP ACTIVATED");

                    _logger.LogInformation(
                        "Membership ID: {MembershipId}",
                        membership.MembershipId);

                    _logger.LogInformation(
                        "Membership Type: {MembershipType}",
                        membership.MembershipType);

                    _logger.LogInformation(
                        "Duration: {DurationMonths} month(s)",
                        durationMonths);

                    _logger.LogInformation(
                        "Start Date: {StartDate}",
                        startDate);

                    _logger.LogInformation(
                        "End Date: {EndDate}",
                        endDate);

                    _logger.LogInformation(
                        "Payment ID: {PaymentId}",
                        mPaymentId);

                    _logger.LogInformation(
                        "========================================");
                }

                
                else if (
                    paymentStatus.Equals(
                        "CANCELLED",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    paymentStatus.Equals(
                        "FAILED",
                        StringComparison.OrdinalIgnoreCase))
                {
                    membership.PaymentStatus =
                        "Failed";

                    payment.PaymentStatus =
                        "Failed";

                    await _context.SaveChangesAsync();

                    _logger.LogWarning(
                        "Payment failed: {PaymentId}, Status: {Status}",
                        mPaymentId,
                        paymentStatus);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing PayFast ITN");

                // PayFast should receive an HTTP response
                // even when an internal error occurs.

                return Ok();
            }
        }

       
        [HttpGet]
        public IActionResult CheckPaymentStatus(
            int membershipId)
        {
            var membership =
                _context.Memberships
                    .FirstOrDefault(
                        m =>
                            m.MembershipId ==
                            membershipId);

            if (membership == null)
            {
                return NotFound();
            }

            return Json(
                new
                {
                    status =
                        membership.PaymentStatus,

                    membershipStatus =
                        membership.Status
                });
        }

      
        [HttpGet]
        public IActionResult PaymentComplete(
            int membershipId)
        {
            try
            {
                var membership =
                    _context.Memberships
                        .FirstOrDefault(
                            m =>
                                m.MembershipId ==
                                membershipId);

                if (membership == null)
                {
                    return NotFound();
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
                    "Error in PaymentComplete for Membership: {MembershipId}",
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