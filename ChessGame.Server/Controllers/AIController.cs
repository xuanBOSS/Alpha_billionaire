using Microsoft.AspNetCore.Mvc;
using ChessGame.AI;
using ChessGame.GameLogic;
[Route("api/ai")]
[ApiController]
public class AIController : ControllerBase
{
    private readonly AIService _aiService;

    public AIController()
    {
        _aiService = new AIService();
    }

    [HttpPost("compute-move")]
    public IActionResult GetAIMove([FromBody] Board board)
    {
        var bestMove = _aiService.GetBestMove(board);
        return Ok(new { aiMove = bestMove });
    }
}
