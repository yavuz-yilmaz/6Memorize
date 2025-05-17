using System.ComponentModel.DataAnnotations;

namespace EnglishApp.Models
{
    public class WordSample
    {
        [Key]
        public int WordSamplesID { get; set; }

        public int WordID { get; set; }

        [Required]
        [StringLength(500)]
        public string Sample { get; set; } = string.Empty;

        // Navigation property
        public virtual Word Word { get; set; } = null!;
    }
} 