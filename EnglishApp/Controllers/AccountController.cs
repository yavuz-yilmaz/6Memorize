using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using _6Memorize.Models;
using _6Memorize.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using _6Memorize.Services;

namespace _6Memorize.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public AccountController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(User model)
        {
            if (ModelState.IsValid)
            {
                // Check if username already exists
                if (await _context.Users.AnyAsync(u => u.UserName == model.UserName))
                {
                    ModelState.AddModelError("UserName", "This username is already taken.");
                    return View(model);
                }

                // Check if email already exists
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "This email is already registered.");
                    return View(model);
                }

                // Hash password
                model.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
                model.CreatedAt = DateTime.UtcNow;
                model.IsEmailConfirmed = false; // Ensure email is not confirmed by default

                _context.Users.Add(model);
                await _context.SaveChangesAsync();

                // Create default settings for the user
                var settings = new UserSettings
                {
                    UserID = model.UserID,
                    WordsPerQuiz = 10
                };
                _context.UserSettings.Add(settings);
                await _context.SaveChangesAsync();

                // Send verification email
                var emailSent = await _emailService.SendVerificationEmail(model.UserID);
        
                if (emailSent)
                {
                    TempData["Message"] = "Registration successful! Please check your email to verify your account.";
                }
                else
                {
                    TempData["Warning"] = "Registration successful, but we couldn't send the verification email. You can request a new one from your profile settings.";
                }

                return RedirectToAction("Login");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Update last login time
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Invalid username or password");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                await _emailService.RequestPasswordReset(email);
            }
            TempData["Message"] = "If an account exists with this email, you will receive password reset instructions.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token)
        {
            var passwordResetToken = await _context.PasswordResetTokens.FirstOrDefaultAsync(t => t.Token == token);
            
            if (passwordResetToken == null)
            {
                TempData["Message"] = "Password reset token does not exist.";
                return RedirectToAction("Login");
            }

            if (passwordResetToken.ExpiryDate < DateTime.UtcNow)
            {
                TempData["Message"] = "Password reset token has expired.";
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == passwordResetToken.UserId);
            if (user == null)
            {
                TempData["Message"] = "User does not exist.";
                return RedirectToAction("Login");
            }
            
            return View(user);
        }
        
        [HttpPost]
        public async Task<IActionResult> ResetPassword(int userId, string token, string newPassword, string confirmPassword)
        {
            // Token validation
            var passwordResetToken = await _context.PasswordResetTokens.FirstOrDefaultAsync(t => t.Token == token);
            if (passwordResetToken == null || passwordResetToken.UserId != userId || passwordResetToken.ExpiryDate < DateTime.UtcNow)
            {
                TempData["Message"] = "Invalid or expired password reset token.";
                return RedirectToAction("Login");
            }

            // Password validation
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 8)
            {
                TempData["Error"] = "Password must be at least 8 characters long.";
                var user = await _context.Users.FindAsync(userId);
                return View(user);
            }

            // Password matching check
            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Passwords don't match.";
                var user = await _context.Users.FindAsync(userId);
                return View(user);
            }

            // Find user and update password
            var userToUpdate = await _context.Users.FindAsync(userId);
            if (userToUpdate == null)
            {
                TempData["Message"] = "User not found.";
                return RedirectToAction("Login");
            }

            // Hash the password
            userToUpdate.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);

            // Delete token
            _context.PasswordResetTokens.Remove(passwordResetToken);

            await _context.SaveChangesAsync();

            TempData["Message"] = "Your password has been reset successfully. You can now login with your new password.";
            return RedirectToAction("Login");
        }
        
        [HttpGet]
        public async Task<IActionResult> VerifyEmail(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Invalid verification token.";
                return RedirectToAction("Login");
            }
    
            var verified = await _emailService.VerifyEmail(token);
    
            if (verified)
            {
                TempData["Message"] = "Your email has been successfully verified. You can now log in.";
            }
            else
            {
                TempData["Error"] = "Email verification failed. The token may be invalid or expired.";
            }
    
            return RedirectToAction("Login");
        }
    }
} 