using _6Memorize.Models;

namespace _6Memorize.Models
{
    public class ProgressViewModel
    {
        public List<UserWordProgress> UserWordProgresses { get; set; } = new();
        public int TotalWords { get; set; }
    }
}
