using Microsoft.EntityFrameworkCore;
using NOTQ.Application.Common.Interfaces;
using NOTQ.Domain.Entities;
using NOTQ.Domain.Enums;

namespace NOTQ.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Child> Children => Set<Child>();
    public DbSet<PracticeSession> PracticeSessions => Set<PracticeSession>();
    public DbSet<PracticeWord> PracticeWords => Set<PracticeWord>();
    public DbSet<AudioAttempt> AudioAttempts => Set<AudioAttempt>();
    public DbSet<AnalysisResult> AnalysisResults => Set<AnalysisResult>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Name).IsRequired().HasMaxLength(100);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(50);

            entity.HasMany(u => u.Children)
                  .WithOne(c => c.Parent)
                  .HasForeignKey(c => c.ParentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.RefreshTokens)
                  .WithOne(r => r.User)
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Child Configuration
        modelBuilder.Entity<Child>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Gender).HasMaxLength(20);

            entity.HasMany(c => c.PracticeSessions)
                  .WithOne(s => s.Child)
                  .HasForeignKey(s => s.ChildId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // PracticeSession Configuration
        modelBuilder.Entity<PracticeSession>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(s => s.Score).HasPrecision(5, 2);

            entity.HasMany(s => s.AudioAttempts)
                  .WithOne(a => a.Session)
                  .HasForeignKey(a => a.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // PracticeWord Configuration
        modelBuilder.Entity<PracticeWord>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.Property(w => w.Word).IsRequired().HasMaxLength(100);
            entity.Property(w => w.ExpectedPronunciation).HasMaxLength(100);
            entity.Property(w => w.ImageUrl).HasMaxLength(500);
            entity.Property(w => w.Difficulty).HasMaxLength(50);
            entity.Property(w => w.TargetSound).HasMaxLength(20);

            entity.HasMany(w => w.AudioAttempts)
                  .WithOne(a => a.Word)
                  .HasForeignKey(a => a.WordId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // AudioAttempt Configuration
        modelBuilder.Entity<AudioAttempt>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.AudioUrl).IsRequired().HasMaxLength(500);

            entity.HasOne(a => a.AnalysisResult)
                  .WithOne(r => r.Attempt)
                  .HasForeignKey<AnalysisResult>(r => r.AttemptId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // AnalysisResult Configuration
        modelBuilder.Entity<AnalysisResult>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Prediction).HasConversion<string>().HasMaxLength(50);
            entity.Property(r => r.IssueType).HasConversion<string>().HasMaxLength(50);
            entity.Property(r => r.DetectedWord).HasMaxLength(100);
            entity.Property(r => r.Confidence).HasPrecision(5, 2);
        });

        // RefreshToken Configuration
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Token).IsRequired().HasMaxLength(255);
            entity.HasIndex(r => r.Token).IsUnique();
        });

        // Seed Practice Words
        SeedPracticeWords(modelBuilder);
    }

    private static void SeedPracticeWords(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PracticeWord>().HasData(
            new PracticeWord
            {
                Id = 1,
                Word = "سمكة",
                ExpectedPronunciation = "samaka",
                ImageUrl = "/images/words/fish.png",
                Difficulty = "Easy",
                TargetSound = "س",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new PracticeWord
            {
                Id = 2,
                Word = "سيارة",
                ExpectedPronunciation = "sayyara",
                ImageUrl = "/images/words/car.png",
                Difficulty = "Easy",
                TargetSound = "س",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new PracticeWord
            {
                Id = 3,
                Word = "شمس",
                ExpectedPronunciation = "shams",
                ImageUrl = "/images/words/sun.png",
                Difficulty = "Easy",
                TargetSound = "ش",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new PracticeWord
            {
                Id = 4,
                Word = "قطة",
                ExpectedPronunciation = "qitta",
                ImageUrl = "/images/words/cat.png",
                Difficulty = "Easy",
                TargetSound = "ق",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new PracticeWord
            {
                Id = 5,
                Word = "كتاب",
                ExpectedPronunciation = "kitab",
                ImageUrl = "/images/words/book.png",
                Difficulty = "Easy",
                TargetSound = "ك",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new PracticeWord
            {
                Id = 6,
                Word = "بطة",
                ExpectedPronunciation = "batta",
                ImageUrl = "/images/words/duck.png",
                Difficulty = "Easy",
                TargetSound = "ب",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new PracticeWord
            {
                Id = 7,
                Word = "تفاحة",
                ExpectedPronunciation = "tuffaha",
                ImageUrl = "/images/words/apple.png",
                Difficulty = "Medium",
                TargetSound = "ت",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new PracticeWord
            {
                Id = 8,
                Word = "أسد",
                ExpectedPronunciation = "asad",
                ImageUrl = "/images/words/lion.png",
                Difficulty = "Easy",
                TargetSound = "س",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new PracticeWord
            {
                Id = 9,
                Word = "جمل",
                ExpectedPronunciation = "jamal",
                ImageUrl = "/images/words/camel.png",
                Difficulty = "Medium",
                TargetSound = "ج",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new PracticeWord
            {
                Id = 10,
                Word = "طائر",
                ExpectedPronunciation = "ta'ir",
                ImageUrl = "/images/words/bird.png",
                Difficulty = "Hard",
                TargetSound = "ط",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
