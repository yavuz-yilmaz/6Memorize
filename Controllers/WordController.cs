using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using _6Memorize.Models;
using _6Memorize.Data;
using System.Security.Claims;

namespace _6Memorize.Controllers
{
    [Authorize]
    public class WordController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public WordController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // Method for category list
        private void PrepareCategories()
        {
            var categories = new List<string>
            {
                "General",
                "Business",
                "Technology",
                "Science",
                "Education",
                "Health",
                "Travel",
                "Sports",
                "Food",
                "Art",
                "Music",
                "Nature",
                "Daily Life"
            };

            ViewBag.Categories = categories;
        }

        // GET: Word
        public async Task<IActionResult> Index()
        {
            var words = await _context.Words
                .Include(w => w.WordSamples)
                .OrderBy(w => w.EngWordName)
                .ToListAsync();
            return View(words);
        }

        // GET: Word/Create
        public IActionResult Create()
        {
            PrepareCategories();
            return View();
        }

        // POST: Word/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Word word, IFormFile? picture, IFormFile? audio, string[]? samples)
        {
            if (ModelState.IsValid)
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

                if (picture != null)
                {
                    word.PicturePath = await SaveFile(picture, "images");
                }

                if (audio != null)
                {
                    word.AudioPath = await SaveFile(audio, "audio");
                }

                // Add example sentences
                if (samples != null && samples.Length > 0)
                {
                    foreach (var sample in samples.Where(s => !string.IsNullOrWhiteSpace(s)))
                    {
                        word.WordSamples.Add(new WordSample
                        {
                            Sample = sample
                        });
                    }
                }

                _context.Add(word);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            PrepareCategories();
            return View(word);
        }

        // GET: Word/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var word = await _context.Words
                .Include(w => w.WordSamples)
                .FirstOrDefaultAsync(w => w.WordID == id);

            if (word == null)
            {
                return NotFound();
            }

            PrepareCategories();
            return View(word);
        }

        // POST: Word/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Word word, IFormFile? picture, IFormFile? audio,
            string[]? samples)
        {
            if (id != word.WordID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingWord = await _context.Words
                        .Include(w => w.WordSamples)
                        .FirstOrDefaultAsync(w => w.WordID == id);

                    if (existingWord == null)
                    {
                        return NotFound();
                    }

                    // Update image and audio files
                    if (picture != null)
                    {
                        // Delete old picture if exists
                        if (!string.IsNullOrEmpty(existingWord.PicturePath))
                        {
                            DeleteFile(existingWord.PicturePath);
                        }

                        existingWord.PicturePath = await SaveFile(picture, "images");
                    }

                    if (audio != null)
                    {
                        // Delete old audio if exists
                        if (!string.IsNullOrEmpty(existingWord.AudioPath))
                        {
                            DeleteFile(existingWord.AudioPath);
                        }

                        existingWord.AudioPath = await SaveFile(audio, "audio");
                    }

                    // Update basic word properties
                    existingWord.EngWordName = word.EngWordName;
                    existingWord.TurWordName = word.TurWordName;
                    existingWord.Category = word.Category;

                    // Update example sentences
                    // First clear existing examples
                    _context.WordSamples.RemoveRange(existingWord.WordSamples);

                    // Then add new examples
                    if (samples != null && samples.Length > 0)
                    {
                        foreach (var sample in samples.Where(s => !string.IsNullOrWhiteSpace(s)))
                        {
                            existingWord.WordSamples.Add(new WordSample
                            {
                                Sample = sample
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!WordExists(word.WordID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            PrepareCategories();
            return View(word);
        }

        // GET: Word/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var word = await _context.Words
                .Include(w => w.WordSamples)
                .FirstOrDefaultAsync(w => w.WordID == id);

            if (word == null)
            {
                return NotFound();
            }

            return View(word);
        }

        // POST: Word/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var word = await _context.Words.FindAsync(id);
            if (word != null)
            {
                // Delete associated files
                if (!string.IsNullOrEmpty(word.PicturePath))
                {
                    DeleteFile(word.PicturePath);
                }

                if (!string.IsNullOrEmpty(word.AudioPath))
                {
                    DeleteFile(word.AudioPath);
                }

                _context.Words.Remove(word);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool WordExists(int id)
        {
            return _context.Words.Any(e => e.WordID == id);
        }

        private async Task<string> SaveFile(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
                return string.Empty;

            var uploadsFolder = Path.Combine(_environment.WebRootPath, folder);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/{folder}/{uniqueFileName}";
        }

        private void DeleteFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }

        // GET: Word/Wordle
        public async Task<IActionResult> Wordle()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = int.Parse(userIdClaim.Value);

            // Get words that the user has made progress on (answered correctly at least once)
            var learnedWords = await _context.UserWordProgresses
                .Include(uwp => uwp.Word)
                .Where(uwp => uwp.UserID == userId && uwp.CurrentStep > 0)
                .Select(uwp => uwp.Word)
                .ToListAsync();

            // If there are no learned words, show a warning message
            if (learnedWords == null || !learnedWords.Any())
            {
                TempData["Warning"] = "You don't have any words to play Wordle yet. Start by working on words in the quiz section.";
                return RedirectToAction("Index", "Word");
            }

            // Select a random word from learned words
            var random = new Random();
            var selectedWord = learnedWords[random.Next(learnedWords.Count)];

            // Create WordleViewModel
            var viewModel = new WordleViewModel
            {
                Word = selectedWord,
                WordLength = selectedWord.EngWordName.Length
            };

            return View(viewModel);
        }
    }
}