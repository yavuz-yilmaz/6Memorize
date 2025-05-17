using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EnglishApp.Models;
using EnglishApp.Data;
using System.Security.Claims;

namespace EnglishApp.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
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
                    Theme = "light"
                };
                _context.UserSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(UserSettings settings)
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
                    existingSettings.Theme = settings.Theme;

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
    }
} 