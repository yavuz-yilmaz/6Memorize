namespace _6Memorize.Models.ViewModels
{
    public class ProfileViewModel
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        
        // Password update properties
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmNewPassword { get; set; }
    }
}