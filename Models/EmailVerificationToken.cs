namespace _6Memorize.Models
{
    public class EmailVerificationToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public DateTime ExpiryDate { get; set; }
    }
}