using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using _6Memorize.Data;
using _6Memorize.Models;
using Microsoft.EntityFrameworkCore;

namespace _6Memorize.Services;

public class EmailService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationDbContext _dbContext;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly bool _enableSsl;
    private readonly string _senderEmail;
    private readonly string _senderName;

    public EmailService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext dbContext, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;

        // Load SMTP settings from configuration and check for null
        _smtpHost = configuration["Smtp:Host"] ?? throw new InvalidOperationException("Smtp:Host configuration is missing");
        _smtpPort = int.Parse(configuration["Smtp:Port"] ?? "587");
        _smtpUsername = configuration["Smtp:Username"] ?? throw new InvalidOperationException("Smtp:Username configuration is missing");
        _smtpPassword = configuration["Smtp:Password"] ?? throw new InvalidOperationException("Smtp:Password configuration is missing");
        _enableSsl = configuration.GetValue<bool>("Smtp:EnableSsl", true);
        _senderEmail = configuration["Smtp:SenderEmail"] ?? "noreply@6Memorize.com";
        _senderName = configuration["Smtp:SenderName"] ?? "6Memorize";
    }

    public string GetBaseUrl()
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null) return "https://6Memorize.com";
        return $"{request.Scheme}://{request.Host}";
    }

    public async Task<bool> RequestPasswordReset(string email)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return false;

        // Create unique token
        var token = GenerateToken();
        var resetToken = new PasswordResetToken
        {
            Token = token,
            UserId = user.UserID,
            ExpiryDate = DateTime.UtcNow.AddHours(1) // Valid for 1 hour
        };

        _dbContext.PasswordResetTokens.Add(resetToken);
        await _dbContext.SaveChangesAsync();

        // Send email
        return await SendResetEmail(user.Email, token);
    }

    public async Task<bool> SendVerificationEmail(int userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null) return false;

        // If email is already verified
        if (user.IsEmailConfirmed) return true;

        // Clear existing verification tokens
        var existingTokens = await _dbContext.EmailVerificationTokens
            .Where(t => t.UserId == userId)
            .ToListAsync();

        if (existingTokens.Any())
        {
            _dbContext.EmailVerificationTokens.RemoveRange(existingTokens);
            await _dbContext.SaveChangesAsync();
        }

        // Create new token
        var token = GenerateToken();
        var verificationToken = new EmailVerificationToken
        {
            Token = token,
            UserId = userId,
            ExpiryDate = DateTime.UtcNow.AddDays(2) // Valid for 2 days
        };

        _dbContext.EmailVerificationTokens.Add(verificationToken);
        await _dbContext.SaveChangesAsync();

        // Send email
        return await SendVerificationEmail(user.Email, token);
    }

    public async Task<bool> VerifyEmail(string token)
    {
        var verificationToken = await _dbContext.EmailVerificationTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token);

        if (verificationToken == null) return false;
        if (verificationToken.ExpiryDate < DateTime.UtcNow) return false;

        verificationToken.User.IsEmailConfirmed = true;
        _dbContext.EmailVerificationTokens.Remove(verificationToken);
        await _dbContext.SaveChangesAsync();

        return true;
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
            mail.From = new MailAddress(_senderEmail, _senderName);
            mail.To.Add(email);
            mail.Subject = "Password Reset Request";
            mail.Body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px;'>
                        <h2 style='color: #3498db;'>6Memorize Password Reset</h2>
                        <p>You've requested to reset your password. Please click the link below to set a new password:</p>
                        <p style='text-align: center;'>
                            <a href='{resetLink}' style='background-color: #3498db; color: white; padding: 10px 15px; text-decoration: none; border-radius: 5px; display: inline-block;'>Reset Password</a>
                        </p>
                        <p>If you didn't request this password reset, you can ignore this email.</p>
                        <p>This link will expire in 1 hour.</p>
                        <hr style='border: none; border-top: 1px solid #ddd;'>
                        <p style='font-size: 12px; color: #777;'>6Memorize - Your language learning assistant</p>
                    </div>
                </body>
                </html>";
            mail.IsBodyHtml = true;

            using var smtpClient = new SmtpClient(_smtpHost, _smtpPort)
            {
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                EnableSsl = _enableSsl
            };

            await smtpClient.SendMailAsync(mail);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> SendVerificationEmail(string email, string token)
    {
        try
        {
            var baseUrl = GetBaseUrl();
            var verificationLink = $"{baseUrl}/Account/VerifyEmail?token={token}";
            var mail = new MailMessage();
            mail.From = new MailAddress(_senderEmail, _senderName);
            mail.To.Add(email);
            mail.Subject = "Verify Your Email Address";
            mail.Body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px;'>
                        <h2 style='color: #3498db;'>Verify Your Email Address</h2>
                        <p>Thank you for registering with 6Memorize! Please click the button below to verify your email address:</p>
                        <p style='text-align: center;'>
                            <a href='{verificationLink}' style='background-color: #3498db; color: white; padding: 10px 15px; text-decoration: none; border-radius: 5px; display: inline-block;'>Verify Email</a>
                        </p>
                        <p>If you didn't create an account with us, you can ignore this email.</p>
                        <p>This link will expire in 48 hours.</p>
                        <hr style='border: none; border-top: 1px solid #ddd;'>
                        <p style='font-size: 12px; color: #777;'>6Memorize - Your language learning assistant</p>
                    </div>
                </body>
                </html>";
            mail.IsBodyHtml = true;

            using var smtpClient = new SmtpClient(_smtpHost, _smtpPort)
            {
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                EnableSsl = _enableSsl
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