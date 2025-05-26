namespace _6Memorize.Models;

public class ProgressReportViewModel
{
    public IEnumerable<UserWordProgress> UserProgress { get; set; }
    public int TotalWords { get; set; }
    public int CompletedWords { get; set; }
    public double CompletionRate { get; set; }
    public List<CategoryProgressViewModel> CategoryProgress { get; set; }
}