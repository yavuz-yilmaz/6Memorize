using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using EnglishApp.Models;
using EnglishApp.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using EnglishApp.Services;

namespace EnglishApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordResetService _passwordResetService;

        public AccountController(ApplicationDbContext context, PasswordResetService passwordResetService)
        {
            _context = context;
            _passwordResetService = passwordResetService;
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

                // Hash password (in production, use proper password hashing)
                model.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
                model.CreatedAt = DateTime.UtcNow;

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
                await _passwordResetService.RequestPasswordReset(email);
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
            // Token kontrolü
            var passwordResetToken = await _context.PasswordResetTokens.FirstOrDefaultAsync(t => t.Token == token);
            if (passwordResetToken == null || passwordResetToken.UserId != userId || passwordResetToken.ExpiryDate < DateTime.UtcNow)
            {
                TempData["Message"] = "Invalid or expired password reset token.";
                return RedirectToAction("Login");
            }

            // Şifre kontrolü
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 8)
            {
                TempData["Error"] = "Password must be at least 8 characters long.";
                var user = await _context.Users.FindAsync(userId);
                return View(user);
            }

            // Şifre eşleşme kontrolü
            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Passwords don't match.";
                var user = await _context.Users.FindAsync(userId);
                return View(user);
            }

            // Kullanıcıyı bul ve şifreyi güncelle
            var userToUpdate = await _context.Users.FindAsync(userId);
            if (userToUpdate == null)
            {
                TempData["Message"] = "User not found.";
                return RedirectToAction("Login");
            }

            // Şifreyi hashleme
            userToUpdate.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
    
            // Token sil
            _context.PasswordResetTokens.Remove(passwordResetToken);
    
            await _context.SaveChangesAsync();
    
            TempData["Message"] = "Your password has been reset successfully. You can now login with your new password.";
            return RedirectToAction("Login");
        }
    }
} 