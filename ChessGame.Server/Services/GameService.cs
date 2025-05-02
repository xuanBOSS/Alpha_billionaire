using ChessGame.GameLogic;

public class GameService
{
    private GameManager _gameManager;

    public GameService()
    {
        _gameManager = new GameManager();
    }

    public bool IsMoveValid(int x, int y)
    {
        /*return _gameManager.IsValidMove(x, y);*/
        return true;
    }
}
