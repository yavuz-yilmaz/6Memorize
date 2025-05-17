using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using EnglishApp.Data;
using EnglishApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishApp.Services;

public class PasswordResetService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationDbContext _dbContext;
    
    public PasswordResetService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
    }
    public string GetBaseUrl()
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null) return "https://EnglishApp.com";
        return $"{request.Scheme}://{request.Host}";
    }
    
    public async Task<bool> RequestPasswordReset(string email)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return false;

        // Benzersiz token oluştur
        var token = GenerateToken();
        var resetToken = new PasswordResetToken
        {
            Token = token,
            UserId = user.UserID,
            ExpiryDate = DateTime.UtcNow.AddHours(1) // 1 saat geçerli
        };

        _dbContext.PasswordResetTokens.Add(resetToken);
        await _dbContext.SaveChangesAsync();

        // E-posta gönder
        return await SendResetEmail(user.Email, token);
    }
    
    private string GenerateToken()
    {
        using (var rng = RandomNumberGenerator.Create())
        {
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_");
        }
    }
    
    private async Task<bool> SendResetEmail(string email, string token)
    {
        try
        {
            var baseUrl = GetBaseUrl();
            var resetLink = $"{baseUrl}/Account/ResetPassword?token={token}";
            var mail = new MailMessage();
            mail.From = new MailAddress("noreply@englishapp.com");
            mail.To.Add(email);
            mail.Subject = "Password Reset Request";
            mail.Body = $"<p>Click link below to reset your password:</p><a href='{resetLink}'>Reset Password</a>";
            mail.IsBodyHtml = true;

            using var smtpClient = new SmtpClient("sandbox.smtp.mailtrap.io", 587)
            {
                Credentials = new NetworkCredential("19ceb0696741b5", "cabf15ffbf7490"),
                EnableSsl = true
            };

            await smtpClient.SendMailAsync(mail);
            return true;
        }
        catch
        {
            return false;
        }
    }
}