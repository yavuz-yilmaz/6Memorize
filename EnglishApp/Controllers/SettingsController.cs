using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using _6Memorize.Models;
using _6Memorize.Data;
using System.Security.Claims;
using _6Memorize.Models.ViewModels;
using _6Memorize.Services;

namespace _6Memorize.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public SettingsController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var settings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.UserID == userId);

            if (settings == null)
            {
                settings = new UserSettings
                {
                    UserID = userId,
                    WordsPerQuiz = 10,
                    ShowAudio = true,
                    ShowImages = true,
                };
                _context.UserSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuizSettings(UserSettings settings)
        {
            if (ModelState.IsValid)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var existingSettings = await _context.UserSettings
                    .FirstOrDefaultAsync(s => s.UserID == userId);

                if (existingSettings == null)
                {
                    settings.UserID = userId;
                    _context.UserSettings.Add(settings);
                }
                else
                {
                    existingSettings.WordsPerQuiz = settings.WordsPerQuiz;
                    existingSettings.ShowAudio = settings.ShowAudio;
                    existingSettings.ShowImages = settings.ShowImages;

                    _context.UserSettings.Update(existingSettings);
                }

                try
                {
                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Settings updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while saving the settings.");
                    return View("Index", settings);
                }
            }

            return View("Index", settings);
        }

        public async Task<IActionResult> Profile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var profileViewModel = new ProfileViewModel
            {
                UserName = user.UserName,
                Email = user.Email
            };

            ViewBag.IsEmailConfirmed = user.IsEmailConfirmed;

            return View(profileViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEmail(string newEmail, string password)
        {
            if (string.IsNullOrWhiteSpace(newEmail) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Email and password are required.";
                return RedirectToAction(nameof(Profile));
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return RedirectToAction("Login", "Account");

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                TempData["Error"] = "Incorrect password.";
                return RedirectToAction(nameof(Profile));
            }

            // Check if email already exists for another user
            if (await _context.Users.AnyAsync(u => u.Email == newEmail && u.UserID != userId))
            {
                TempData["Error"] = "This email is already registered with another account.";
                return RedirectToAction(nameof(Profile));
            }

            // Update email and set as unconfirmed
            user.Email = newEmail;
            user.IsEmailConfirmed = false;
            await _context.SaveChangesAsync();

            // Send verification email
            await _emailService.SendVerificationEmail(userId);

            TempData["Message"] = "Email updated successfully. Please check your inbox for verification.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(string currentPassword, string newPassword,
            string confirmNewPassword)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return RedirectToAction("Login", "Account");

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.Password))
            {
                TempData["Error"] = "Current password is incorrect.";
                return RedirectToAction(nameof(Profile));
            }

            // Validate new password
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 8)
            {
                TempData["Error"] = "New password must be at least 8 characters long.";
                return RedirectToAction(nameof(Profile));
            }

            // Confirm passwords match
            if (newPassword != confirmNewPassword)
            {
                TempData["Error"] = "New passwords don't match.";
                return RedirectToAction(nameof(Profile));
            }

            // Update password
            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Password updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        public async Task<JsonResult> ResendVerificationEmail()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return Json(new { success = false, message = "User not found" });

            if (user.IsEmailConfirmed)
                return Json(new { success = false, message = "Email already verified" });

            var result = await _emailService.SendVerificationEmail(userId);

            if (result)
                return Json(new { success = true });
            else
                return Json(new { success = false, message = "Failed to send email" });
        }
    }
}