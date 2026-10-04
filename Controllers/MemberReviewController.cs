using System.Security.Claims;
using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DUT_Campus_FIT_Gym.Controllers
{
    [Authorize(Roles = "Student,Staff")]
    public class MemberReviewController : Controller
    {
        private readonly GymDbContext _context;

        public MemberReviewController(GymDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var memberId = GetMemberId();

            if (memberId == null)
                return RedirectToAction("Login", "Account");

            var reviews = await _context.MemberReviews
                .Include(r => r.Member)
                .OrderByDescending(r => r.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            var myReview = reviews.FirstOrDefault(r => r.MemberId == memberId.Value);

            ViewBag.MyReview = myReview;
            ViewBag.AverageRating = reviews.Any()
                ? reviews.Average(r => r.Rating)
                : 0;

            ViewBag.TotalReviews = reviews.Count;
            ViewBag.FiveStar = reviews.Count(r => r.Rating == 5);
            ViewBag.FourStar = reviews.Count(r => r.Rating == 4);
            ViewBag.ThreeStar = reviews.Count(r => r.Rating == 3);
            ViewBag.TwoStar = reviews.Count(r => r.Rating == 2);
            ViewBag.OneStar = reviews.Count(r => r.Rating == 1);

            return View(reviews);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int rating, string reviewText)
        {
            var memberId = GetMemberId();

            if (memberId == null)
                return RedirectToAction("Login", "Account");

            if (rating < 1 || rating > 5)
            {
                TempData["ReviewError"] = "Please select a rating between 1 and 5 stars.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(reviewText))
            {
                TempData["ReviewError"] = "Please enter a review.";
                return RedirectToAction(nameof(Index));
            }

            if (reviewText.Trim().Length > 1000)
            {
                TempData["ReviewError"] = "Your review cannot exceed 1000 characters.";
                return RedirectToAction(nameof(Index));
            }

            var existingReview = await _context.MemberReviews
                .FirstOrDefaultAsync(r => r.MemberId == memberId.Value);

            if (existingReview != null)
            {
                TempData["ReviewError"] = "You have already submitted a review. You can edit your existing review instead.";
                return RedirectToAction(nameof(Index));
            }

            var review = new MemberReview
            {
                MemberId = memberId.Value,
                Rating = rating,
                ReviewText = reviewText.Trim(),
                CreatedAt = DateTime.Now
            };

            _context.MemberReviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["ReviewSuccess"] = "Your review has been submitted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, int rating, string reviewText)
        {
            var memberId = GetMemberId();

            if (memberId == null)
                return RedirectToAction("Login", "Account");

            var review = await _context.MemberReviews
                .FirstOrDefaultAsync(r => r.MemberReviewId == id && r.MemberId == memberId.Value);

            if (review == null)
            {
                TempData["ReviewError"] = "Review not found.";
                return RedirectToAction(nameof(Index));
            }

            if (rating < 1 || rating > 5)
            {
                TempData["ReviewError"] = "Please select a rating between 1 and 5 stars.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(reviewText))
            {
                TempData["ReviewError"] = "Please enter a review.";
                return RedirectToAction(nameof(Index));
            }

            if (reviewText.Trim().Length > 1000)
            {
                TempData["ReviewError"] = "Your review cannot exceed 1000 characters.";
                return RedirectToAction(nameof(Index));
            }

            review.Rating = rating;
            review.ReviewText = reviewText.Trim();
            review.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["ReviewSuccess"] = "Your review has been updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var memberId = GetMemberId();

            if (memberId == null)
                return RedirectToAction("Login", "Account");

            var review = await _context.MemberReviews
                .FirstOrDefaultAsync(r => r.MemberReviewId == id && r.MemberId == memberId.Value);

            if (review == null)
            {
                TempData["ReviewError"] = "Review not found.";
                return RedirectToAction(nameof(Index));
            }

            _context.MemberReviews.Remove(review);
            await _context.SaveChangesAsync();

            TempData["ReviewSuccess"] = "Your review has been deleted.";
            return RedirectToAction(nameof(Index));
        }

        private int? GetMemberId()
        {
            var memberIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(memberIdClaim, out var memberId))
                return memberId;

            return null;
        }
    }
}