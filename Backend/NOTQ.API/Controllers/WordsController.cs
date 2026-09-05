using Microsoft.AspNetCore.Mvc;
using NOTQ.Application.Common.Models;
using NOTQ.Application.DTOs.Words;
using NOTQ.Application.Interfaces;

namespace NOTQ.API.Controllers;

public class WordsController : BaseApiController
{
    private readonly IPracticeWordService _wordService;

    public WordsController(IPracticeWordService wordService)
    {
        _wordService = wordService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PracticeWordDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWords(
        [FromQuery] string? difficulty,
        [FromQuery] string? targetSound,
        CancellationToken cancellationToken)
    {
        var words = await _wordService.GetAllWordsAsync(difficulty, targetSound, cancellationToken);
        return Ok(ApiResponse<IEnumerable<PracticeWordDto>>.Ok(words));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<PracticeWordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWordById(int id, CancellationToken cancellationToken)
    {
        var word = await _wordService.GetWordByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<PracticeWordDto>.Ok(word));
    }
}
