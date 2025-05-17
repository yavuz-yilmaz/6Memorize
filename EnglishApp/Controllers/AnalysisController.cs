using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EnglishApp.Models;
using EnglishApp.Data;
using System.Security.Claims;

namespace EnglishApp.Controllers
{
    [Authorize]
    public class AnalysisController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AnalysisController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Kategori bazlı ilerleme analizi
            var categoryProgress =
                await _context.Words.GroupBy(w => w.Category)
                    .Select(g => new CategoryProgressViewModel
                    {
                        Category = g.Key,
                        TotalWords = g.Count(),
                        KnownWords = _context.UserWordProgresses
                            .Include(uwp => uwp.Word)
                            .Where(uwp => uwp.UserID == userId && uwp.Word.Category == g.Key && uwp.IsCompleted == true).Count(),
                        SuccessRate = (double) _context.UserWordProgresses
                            .Include(uwp => uwp.Word)
                            .Where(uwp => uwp.UserID == userId && uwp.Word.Category == g.Key && uwp.IsCompleted == true).Count() / g.Count() * 100
                    })
                    .ToListAsync();

            // Genel istatistikler
            var totalWords = await _context.Words.CountAsync();

            var knownWords = await _context.UserWordProgresses
                .Where(uwp => uwp.UserID == userId && uwp.IsCompleted == true)
                .CountAsync();

            var viewModel = new AnalysisViewModel
            {
                CategoryProgress = categoryProgress,
                TotalWords = totalWords,
                KnownWords = knownWords,
                OverallSuccessRate = totalWords > 0 ? (double)knownWords / totalWords * 100 : 0
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Print()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Kategori bazlı ilerleme analizi
            var categoryProgress =
                await _context.Words.GroupBy(w => w.Category)
                    .Select(g => new CategoryProgressViewModel
                    {
                        Category = g.Key,
                        TotalWords = g.Count(),
                        KnownWords = _context.UserWordProgresses
                            .Include(uwp => uwp.Word)
                            .Where(uwp => uwp.UserID == userId && uwp.Word.Category == g.Key && uwp.IsCompleted == true).Count(),
                        SuccessRate = (double) _context.UserWordProgresses
                            .Include(uwp => uwp.Word)
                            .Where(uwp => uwp.UserID == userId && uwp.Word.Category == g.Key && uwp.IsCompleted == true).Count() / g.Count() * 100
                    })
                    .ToListAsync();

            // Genel istatistikler
            var totalWords = await _context.Words.CountAsync();

            var knownWords = await _context.UserWordProgresses
                .Where(uwp => uwp.UserID == userId && uwp.IsCompleted == true)
                .CountAsync();

            var viewModel = new AnalysisViewModel
            {
                CategoryProgress = categoryProgress,
                TotalWords = totalWords,
                KnownWords = knownWords,
                OverallSuccessRate = totalWords > 0 ? (double)knownWords / totalWords * 100 : 0
            };


            return View("Print", viewModel);
        }
    }
} 