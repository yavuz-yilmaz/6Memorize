using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EnglishApp.Models;
using EnglishApp.Data;
using System.Security.Claims;
using System.Linq;
using System.Collections.Generic;
using System;

namespace EnglishApp.Controllers
{
    [Authorize]
    public class QuizController : Controller
    {
        private readonly ApplicationDbContext _context;

        public QuizController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = int.Parse(userIdClaim.Value);
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userSettings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.UserID == userId);

            if (userSettings == null)
            {
                userSettings = new UserSettings
                {
                    UserID = userId,
                    WordsPerQuiz = 10,
                    ShowAudio = true,
                    ShowImages = true,
                    Theme = "light"
                };
                _context.UserSettings.Add(userSettings);
                await _context.SaveChangesAsync();
            }
            // Kaç yeni kelime alınacak
            var wordsPerQuiz = userSettings.WordsPerQuiz;

            // 1. Önceki günlerden gelen ve tekrar edilmesi gereken kelimeleri al
            var dueWords = await _context.UserWordProgresses
                .Include(uwp => uwp.Word)
                .ThenInclude(w => w.WordSamples)
                .Where(uwp => uwp.UserID == userId &&
                              uwp.NextDueDate <= DateTime.UtcNow &&
                              !uwp.IsCompleted && uwp.CurrentStep > 0)
                .OrderBy(uwp => uwp.NextDueDate)
                .ToListAsync();

            if (wordsPerQuiz > 0)
            {
                // Şu anda kullanıcının progress kaydı olan kelimelerin ID'lerini al
                var existingWordIds = await _context.UserWordProgresses
                    .Where(uwp => uwp.UserID == userId)
                    .Select(uwp => uwp.WordID)
                    .ToListAsync();

                // 2. Henüz öğrenilmemiş yeni kelimeleri getir
                var newWords = await _context.Words
                    .Include(w => w.WordSamples)
                    .Where(w => !existingWordIds.Contains(w.WordID))
                    .OrderBy(w => EF.Functions.Random())
                    .Take(wordsPerQuiz)
                    .ToListAsync();

                // Yeni kelimeler için progress kaydı oluştur
                foreach (var word in newWords)
                {
                    var progress = new UserWordProgress
                    {
                        UserID = userId,
                        WordID = word.WordID,
                        CurrentStep = 0,
                        NextDueDate = DateTime.UtcNow,
                        IsCompleted = false,
                        LastAttemptDate = DateTime.UtcNow,
                        LastAttemptSuccess = false,
                        Word = word
                    };
                    _context.UserWordProgresses.Add(progress);
                    dueWords.Add(progress);
                }

                if (newWords.Any())
                {
                    await _context.SaveChangesAsync();
                }

                // 3. Yeterli yeni kelime yoksa, mevcut ama tamamlanmamış kelimelerden ekle
                if (dueWords.Count < wordsPerQuiz)
                {
                    var additionalWords = await _context.UserWordProgresses
                        .Include(uwp => uwp.Word)
                        .ThenInclude(w => w.WordSamples)
                        .Where(uwp => uwp.UserID == userId &&
                                      !uwp.IsCompleted &&
                                      uwp.CurrentStep == 0 &&
                                      !dueWords.Select(d => d.ID).Contains(uwp.ID))
                        .OrderBy(uwp => EF.Functions.Random())
                        .Take(wordsPerQuiz - dueWords.Count)
                        .ToListAsync();

                    dueWords.AddRange(additionalWords);
                }
            }

            // Kelimeleri karıştır
            var random = new Random();
            dueWords = dueWords.OrderBy(x => random.Next()).ToList();

            // Sadece ID'leri göndermek için liste oluştur
            var wordProgressIds = dueWords.Select(w => w.ID).ToList();
            
            ViewBag.UserSettings = userSettings;
            
            return View(wordProgressIds);
        }

        [HttpPost]
        public async Task<IActionResult> GetWordProcess(int id)
        {
            var process = await _context.UserWordProgresses
                .Include(uwp => uwp.Word)
                .ThenInclude(w => w.WordSamples)
                .FirstOrDefaultAsync(uwp => uwp.ID == id);
            
            if (process == null)
            {
                return Json(new { success = false });
            }

            return Json(new
            {
                success = true,
                userId = process.UserID,
                wordId = process.WordID,
                engWordName = process.Word.EngWordName,
                turWordName = process.Word.TurWordName,
                picturePath = process.Word.PicturePath,
                category = process.Word.Category,
                audioPath = process.Word.AudioPath,
                createdAt = process.Word.CreatedAt,
                currentStep = process.CurrentStep,
                nextDueDate = process.NextDueDate,
                isCompleted = process.IsCompleted,
                lastAttemptDate = process.LastAttemptDate,
                lastAttemptSuccess = process.LastAttemptSuccess,
                samples = process.Word.WordSamples.Select(ws => ws.Sample).ToList() // Örnekleri yanıta ekleyin
            });
        }

        [HttpPost]
        public async Task<IActionResult> SubmitAnswer(int wordId, string answer)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var userId = int.Parse(userIdClaim.Value);

                //Is word exist
                var word = await _context.Words.FirstOrDefaultAsync(w => w.WordID == wordId);
                if (word == null)
                {
                    return Json(new { success = false, message = "Word not found" });
                }

                var progress = await _context.UserWordProgresses
                    .Include(uwp => uwp.Word)
                    .FirstOrDefaultAsync(uwp => uwp.UserID == userId && uwp.WordID == wordId);

                if (progress == null)
                {
                    return Json(new { success = false, message = "Word progress not found" });
                }

                if (!String.IsNullOrEmpty(answer))
                {
                    var isCorrect = answer.Trim().Equals(progress.Word.TurWordName, StringComparison.OrdinalIgnoreCase);
                    progress.LastAttemptDate = DateTime.UtcNow;
                    progress.LastAttemptSuccess = isCorrect;

                    if (isCorrect)
                    {
                        progress.CurrentStep++;
                        if (progress.CurrentStep >= 6)
                        {
                            progress.IsCompleted = true;
                        }
                        else
                        {
                            progress.NextDueDate = CalculateNextDueDate(progress.CurrentStep);
                        }
                    }
                    else
                    {
                        progress.CurrentStep = 0;
                        progress.NextDueDate = DateTime.UtcNow.AddDays(1);
                    }

                    await _context.SaveChangesAsync();
                    return Json(new
                    {
                        success = isCorrect,
                        nextDueDate = progress.NextDueDate,
                        isCompleted = progress.IsCompleted,
                        currentStep = progress.CurrentStep,
                        correctAnswer = progress.Word.TurWordName
                    });
                }

                return Json(new { success = false, message = "No answer entered", correctAnswer = progress.Word.TurWordName });
            }
            catch (Exception ex)
            {
                // Log the error
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        private DateTime CalculateNextDueDate(int currentStep)
        {
            return currentStep switch
            {
                0 => DateTime.UtcNow, // İlk görüşme
                1 => DateTime.UtcNow.AddDays(1), // 1 gün sonra
                2 => DateTime.UtcNow.AddDays(7), // 1 hafta sonra
                3 => DateTime.UtcNow.AddDays(30), // 1 ay sonra
                4 => DateTime.UtcNow.AddDays(90), // 3 ay sonra
                5 => DateTime.UtcNow.AddDays(180), // 6 ay sonra
                6 => DateTime.UtcNow.AddDays(365), // 1 yıl sonra
                _ => DateTime.UtcNow // Varsayılan olarak şimdi
            };
        }

        public async Task<IActionResult> Progress()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var userId = int.Parse(userIdClaim.Value);
                var progress = await _context.UserWordProgresses
                    .Include(uwp => uwp.Word)
                    .Where(uwp => uwp.UserID == userId && uwp.CurrentStep != 0)
                    .OrderByDescending(uwp => uwp.LastAttemptDate)
                    .ToListAsync();

                if (!progress.Any())
                {
                    TempData["Message"] = "You haven't started learning any words yet. Start a quiz to begin!";
                }

                return View(progress);
            }
            catch (Exception ex)
            {
                // Log the error
                TempData["Error"] = "An error occurred while loading your progress. Please try again.";
                return RedirectToAction("Index");
            }
        }
    }
}