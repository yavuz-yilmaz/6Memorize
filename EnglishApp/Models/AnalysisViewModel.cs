namespace EnglishApp.Models
{
    public class AnalysisViewModel
    {
        public List<CategoryProgressViewModel> CategoryProgress { get; set; } = new();
        public int TotalWords { get; set; }
        public int KnownWords { get; set; }
        public double OverallSuccessRate { get; set; }
        public string? UserName { get; set; }
    }

    public class CategoryProgressViewModel
    {
        public string Category { get; set; } = null!;
        public int TotalWords { get; set; }
        public int KnownWords { get; set; }
        public double SuccessRate { get; set; }
    }
    
} 