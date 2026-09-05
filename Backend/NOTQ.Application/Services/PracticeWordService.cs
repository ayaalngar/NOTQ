using Microsoft.EntityFrameworkCore;
using NOTQ.Application.Common.Exceptions;
using NOTQ.Application.Common.Interfaces;
using NOTQ.Application.DTOs.Words;
using NOTQ.Application.Interfaces;

namespace NOTQ.Application.Services;

public class PracticeWordService : IPracticeWordService
{
    private readonly IApplicationDbContext _context;

    public PracticeWordService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PracticeWordDto>> GetAllWordsAsync(
        string? difficulty = null,
        string? targetSound = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PracticeWords.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(difficulty))
        {
            query = query.Where(w => w.Difficulty.ToLower() == difficulty.Trim().ToLower());
        }

        if (!string.IsNullOrWhiteSpace(targetSound))
        {
            query = query.Where(w => w.TargetSound != null && w.TargetSound == targetSound.Trim());
        }

        var words = await query.OrderBy(w => w.Id).ToListAsync(cancellationToken);

        return words.Select(w => new PracticeWordDto
        {
            Id = w.Id,
            Word = w.Word,
            ExpectedPronunciation = w.ExpectedPronunciation,
            ImageUrl = w.ImageUrl,
            Difficulty = w.Difficulty,
            TargetSound = w.TargetSound
        });
    }

    public async Task<PracticeWordDto> GetWordByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var word = await _context.PracticeWords
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (word == null)
        {
            throw new NotFoundException("PracticeWord", id);
        }

        return new PracticeWordDto
        {
            Id = word.Id,
            Word = word.Word,
            ExpectedPronunciation = word.ExpectedPronunciation,
            ImageUrl = word.ImageUrl,
            Difficulty = word.Difficulty,
            TargetSound = word.TargetSound
        };
    }
}
