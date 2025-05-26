using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using _6Memorize.Models;
using _6Memorize.Data;
using System.Security.Claims;
using System.Linq;
using System.Collections.Generic;
using System;

namespace _6Memorize.Controllers
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
                    ShowImages = true
                };
                _context.UserSettings.Add(userSettings);
                await _context.SaveChangesAsync();
            }
            // Number of new words to get
            var wordsPerQuiz = userSettings.WordsPerQuiz;

            // 1. Get words from previous days that need to be reviewed
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
                // Get IDs of words that the user already has progress records for
                var existingWordIds = await _context.UserWordProgresses
                    .Where(uwp => uwp.UserID == userId)
                    .Select(uwp => uwp.WordID)
                    .ToListAsync();

                // 2. Get new words that haven't been learned yet
                var newWords = await _context.Words
                    .Include(w => w.WordSamples)
                    .Where(w => !existingWordIds.Contains(w.WordID))
                    .OrderBy(w => EF.Functions.Random())
                    .Take(wordsPerQuiz)
                    .ToListAsync();

                // Create progress records for new words
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

                // 3. If there aren't enough new words, add existing but incomplete words
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

            // Shuffle the words
            var random = new Random();
            dueWords = dueWords.OrderBy(x => random.Next()).ToList();

            // Create a list to send only IDs
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
                samples = process.Word.WordSamples.Select(ws => ws.Sample).ToList() // Add examples to the response
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

                // Check if word exists
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
                    var isCorrect = answer.Trim().Equals(progress.Word.EngWordName, StringComparison.OrdinalIgnoreCase);
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
                        correctAnswer = progress.Word.EngWordName
                    });
                }
                return Json(new { success = false, message = "No answer entered", correctAnswer = progress.Word.EngWordName });
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
                0 => DateTime.UtcNow, // First encounter
                1 => DateTime.UtcNow.AddDays(1), // After 1 day
                2 => DateTime.UtcNow.AddDays(7), // After 1 week
                3 => DateTime.UtcNow.AddDays(30), // After 1 month
                4 => DateTime.UtcNow.AddDays(90), // After 3 months
                5 => DateTime.UtcNow.AddDays(180), // After 6 months
                6 => DateTime.UtcNow.AddDays(365), // After 1 year
                _ => DateTime.UtcNow // Default is now
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
                var progresses = await _context.UserWordProgresses
                    .Include(uwp => uwp.Word)
                    .Where(uwp => uwp.UserID == userId && uwp.CurrentStep != 0)
                    .OrderByDescending(uwp => uwp.LastAttemptDate)
                    .ToListAsync();

                if (!progresses.Any())
                {
                    TempData["Message"] = "You haven't started learning any words yet. Start a quiz to begin!";
                }

                var progressViewModel = new ProgressViewModel
                {
                    UserWordProgresses = progresses,
                    TotalWords = await _context.Words.CountAsync()
                };
                return View(progressViewModel);
            }
            catch (Exception ex)
            {
                // Log the error
                TempData["Error"] = "An error occurred while loading your progress. Please try again.";
                return RedirectToAction("Index");
            }
        }
        
        public async Task<IActionResult> PrintReport()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = int.Parse(userIdClaim.Value);
    
            // Get user progress information
            var progress = await _context.UserWordProgresses
                .Include(uwp => uwp.Word)
                .Where(uwp => uwp.UserID == userId && uwp.CurrentStep != 0)
                .OrderByDescending(uwp => uwp.LastAttemptDate)
                .ToListAsync();

            // Progress metrics
            int totalWords = await _context.Words.CountAsync();
            int completedWords = progress.Count(m => m.IsCompleted);
            double completionRate = totalWords > 0 ? (double)completedWords / totalWords * 100 : 0;

            // Category-based progress analysis
            var categoryProgress = await _context.Words
                .GroupJoin(
                    _context.UserWordProgresses.Where(uwp => uwp.UserID == userId && uwp.CurrentStep > 0),
                    word => word.WordID,
                    uwp => uwp.WordID,
                    (word, uwps) => new { Word = word, Progresses = uwps })
                .GroupBy(x => x.Word.Category)
                .Select(g => new CategoryProgressViewModel
                {
                    Category = g.Key,
                    TotalWords = g.Count(),
                    KnownWords = g.Sum(x => x.Progresses.Count(p => p.IsCompleted)),
                    SuccessRate = g.Count() > 0 ? 
                        (double)g.Sum(x => x.Progresses.Count(p => p.IsCompleted)) / g.Count() * 100 : 0
                })
                .ToListAsync();

            var model = new ProgressReportViewModel
            {
                UserProgress = progress,
                TotalWords = totalWords,
                CompletedWords = completedWords,
                CompletionRate = completionRate,
                CategoryProgress = categoryProgress
            };

            return View(model);
        }
    }
}