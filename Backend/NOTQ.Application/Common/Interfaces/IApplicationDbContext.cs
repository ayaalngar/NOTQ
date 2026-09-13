using Microsoft.EntityFrameworkCore;
using NOTQ.Domain.Entities;

namespace NOTQ.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Child> Children { get; }
    DbSet<Word> Words { get; }
    DbSet<Session> Sessions { get; }
    DbSet<Attempt> Attempts { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
