using System.ComponentModel.DataAnnotations;

namespace _6Memorize.Models
{
    public class Word
    {
        [Key]
        public int WordID { get; set; }

        [Required]
        [StringLength(100)]
        public string EngWordName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string TurWordName { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = "General";

        [StringLength(200)]
        public string? PicturePath { get; set; }

        [StringLength(200)]
        public string? AudioPath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<WordSample> WordSamples { get; set; } = new List<WordSample>();
        public virtual ICollection<UserWordProgress> UserWordProgresses { get; set; } = new List<UserWordProgress>();
    }
}