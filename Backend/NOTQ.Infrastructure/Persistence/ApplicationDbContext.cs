using Microsoft.EntityFrameworkCore;
using NOTQ.Application.Common.Interfaces;
using NOTQ.Domain.Entities;

namespace NOTQ.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Child> Children => Set<Child>();
    public DbSet<Word> Words => Set<Word>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Attempt> Attempts => Set<Attempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Child>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Age).IsRequired();
            entity.Property(c => c.AvatarId).IsRequired().HasMaxLength(100);

            entity.HasMany(c => c.Sessions)
                  .WithOne(s => s.Child)
                  .HasForeignKey(s => s.ChildId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Word>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.Property(w => w.WordText).HasColumnName("Word").IsRequired().HasMaxLength(100);
            entity.Property(w => w.Type).IsRequired().HasMaxLength(20);
            entity.Property(w => w.ImageUrl).IsRequired().HasMaxLength(500);
            entity.Property(w => w.CategoryLetter).HasMaxLength(10);

            entity.HasMany(w => w.Attempts)
                  .WithOne(a => a.Word)
                  .HasForeignKey(a => a.WordId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Type).IsRequired().HasMaxLength(50);
            entity.Property(s => s.Status).IsRequired().HasMaxLength(50);
            entity.Property(s => s.Score);

            entity.HasMany(s => s.Attempts)
                  .WithOne(a => a.Session)
                  .HasForeignKey(a => a.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Attempt>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.AudioUrl).IsRequired().HasMaxLength(500);
            entity.Property(a => a.Prediction).HasMaxLength(50);
            entity.Property(a => a.IssueType).HasMaxLength(50);
            entity.Property(a => a.DetectedWord).HasMaxLength(100);
            entity.Property(a => a.FeedbackType).HasMaxLength(50);
            entity.Property(a => a.FeedbackMessage).HasMaxLength(500);
        });

        SeedWords(modelBuilder);
    }

    private static void SeedWords(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Word>().HasData(
            // Letters (Type = "Letter")
            new Word
            {
                Id = 1,
                WordText = "ك",
                Type = "Letter",
                ImageUrl = "/assets/letters/kaf.png",
                CategoryLetter = "ك",
                CreatedAt = seedDate
            },
            new Word
            {
                Id = 2,
                WordText = "ر",
                Type = "Letter",
                ImageUrl = "/assets/letters/raa.png",
                CategoryLetter = "ر",
                CreatedAt = seedDate
            },
            new Word
            {
                Id = 3,
                WordText = "س",
                Type = "Letter",
                ImageUrl = "/assets/letters/seen.png",
                CategoryLetter = "س",
                CreatedAt = seedDate
            },
            new Word
            {
                Id = 4,
                WordText = "ش",
                Type = "Letter",
                ImageUrl = "/assets/letters/sheen.png",
                CategoryLetter = "ش",
                CreatedAt = seedDate
            },

            // Words (Type = "Word")
            new Word
            {
                Id = 5,
                WordText = "كلب",
                Type = "Word",
                ImageUrl = "/assets/words/dog.png",
                CategoryLetter = "ك",
                CreatedAt = seedDate
            },
            new Word
            {
                Id = 6,
                WordText = "قرد",
                Type = "Word",
                ImageUrl = "/assets/words/monkey.png",
                CategoryLetter = "ق",
                CreatedAt = seedDate
            },
            new Word
            {
                Id = 7,
                WordText = "أسد",
                Type = "Word",
                ImageUrl = "/assets/words/lion.png",
                CategoryLetter = "س",
                CreatedAt = seedDate
            }
        );
    }
}
