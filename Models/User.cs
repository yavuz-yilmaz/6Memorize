using System.ComponentModel.DataAnnotations;

namespace _6Memorize.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        [StringLength(50)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        public bool IsEmailConfirmed { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<UserWordProgress> UserWordProgresses { get; set; } = new List<UserWordProgress>();
        public virtual ICollection<WordPuzzle> WordPuzzles { get; set; } = new List<WordPuzzle>();
        public virtual ICollection<WordChainStory> WordChainStories { get; set; } = new List<WordChainStory>();
    }
} 