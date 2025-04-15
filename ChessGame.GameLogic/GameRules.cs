namespace ChessGame.GameLogic
{
    public class GameRules
    {
        public static bool TestValid(int x,int y,string color,Board board)//判断落子合法性
        {
            return true;
        }

        //和棋或者胜利或未结束
        public static string TestEnd(int x,int y,string color,Board board)//判断落子后是否胜利
        {
            //return "CONTINUE";//对局继续
            //return "DRAW";//和棋
            return "WIN";//胜利
        }
    }
}