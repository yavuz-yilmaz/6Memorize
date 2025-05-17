using Microsoft.EntityFrameworkCore;
using EnglishApp.Models;

namespace EnglishApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Word> Words { get; set; }
        public DbSet<WordSample> WordSamples { get; set; }
        public DbSet<UserWordProgress> UserWordProgresses { get; set; }
        public DbSet<UserSettings> UserSettings { get; set; }
        public DbSet<WordPuzzle> WordPuzzles { get; set; }
        public DbSet<WordChainStory> WordChainStories { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<UserWordProgress>()
                .HasOne(uwp => uwp.User)
                .WithMany(u => u.UserWordProgresses)
                .HasForeignKey(uwp => uwp.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserWordProgress>()
                .HasOne(uwp => uwp.Word)
                .WithMany(w => w.UserWordProgresses)
                .HasForeignKey(uwp => uwp.WordID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WordSample>()
                .HasOne(ws => ws.Word)
                .WithMany(w => w.WordSamples)
                .HasForeignKey(ws => ws.WordID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserSettings>()
                .HasOne(us => us.User)
                .WithOne()
                .HasForeignKey<UserSettings>(us => us.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WordPuzzle>()
                .HasOne(wp => wp.User)
                .WithMany(u => u.WordPuzzles)
                .HasForeignKey(wp => wp.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WordPuzzle>()
                .HasOne(wp => wp.Word)
                .WithMany()
                .HasForeignKey(wp => wp.WordID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WordChainStory>()
                .HasOne(wcs => wcs.User)
                .WithMany(u => u.WordChainStories)
                .HasForeignKey(wcs => wcs.UserID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
} 