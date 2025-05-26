using System.ComponentModel.DataAnnotations;

namespace _6Memorize.Models
{
    public class WordPuzzle
    {
        [Key]
        public int ID { get; set; }

        public int UserID { get; set; }
        public int WordID { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsCompleted { get; set; }
        public int Attempts { get; set; }
        public string? GuessedLetters { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Word Word { get; set; } = null!;
    }
} 