using System.ComponentModel.DataAnnotations;

namespace _6Memorize.Models
{
    public class UserWordProgress
    {
        [Key]
        public int ID { get; set; }

        public int UserID { get; set; }
        public int WordID { get; set; }
        public int CurrentStep { get; set; } // 0-6
        public DateTime NextDueDate { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime LastAttemptDate { get; set; }
        public bool LastAttemptSuccess { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Word Word { get; set; } = null!;
    }
} 