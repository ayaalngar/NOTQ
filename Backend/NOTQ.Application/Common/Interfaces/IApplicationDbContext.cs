using Microsoft.EntityFrameworkCore;
using NOTQ.Domain.Entities;

namespace NOTQ.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Child> Children { get; }
    DbSet<PracticeSession> PracticeSessions { get; }
    DbSet<PracticeWord> PracticeWords { get; }
    DbSet<AudioAttempt> AudioAttempts { get; }
    DbSet<AnalysisResult> AnalysisResults { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
