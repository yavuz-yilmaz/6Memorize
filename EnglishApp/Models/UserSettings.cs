using System.ComponentModel.DataAnnotations;

namespace _6Memorize.Models
{
    public class UserSettings
    {
        [Key]
        public int ID { get; set; }

        public int UserID { get; set; }

        [Required]
        [Range(1, 50, ErrorMessage = "Words per quiz must be between 1 and 50")]
        public int WordsPerQuiz { get; set; } = 10;

        public bool ShowAudio { get; set; } = true;
        public bool ShowImages { get; set; } = true;

        // Navigation property
        public virtual User? User { get; set; } = null;
    }
} 