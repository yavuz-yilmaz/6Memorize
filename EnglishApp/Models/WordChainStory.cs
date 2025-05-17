using System.ComponentModel.DataAnnotations;

namespace EnglishApp.Models
{
    public class WordChainStory
    {
        [Key]
        public int ID { get; set; }

        public int UserID { get; set; }
        public string WordChain { get; set; } = string.Empty;
        public string GeneratedStory { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual User User { get; set; } = null!;
    }
} 