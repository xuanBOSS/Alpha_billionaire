using ChessGame.AI;
using ChessGame.GameLogic;
public class AIService
{
    private MinimaxAI _ai;

    public AIService()
    {
        _ai = new MinimaxAI();
    }

    public Move GetBestMove(ChessGame.GameLogic.Board board)
    {
        return _ai.CalculateBestMove(board);
    }
    
}
