using ChessGame.GameLogic;

namespace ChessGame.Server.Controllers
{
    public class Room//除了玩家和房间信息外，还需要维护一个棋盘
    {
        public string RoomID {  get;private set; }//房间ID
        public string Player1 { get; set; }//玩家1的ID
        public string Player2 {  get; set; }//玩家2的ID
        public bool IsFull {  get; set; }//房间是否已满
        public bool IfEnd {  get; set; }//对局是否结束
        public Board board { get; set; }//棋盘

        public Room(string roomid,string player1)//在RoomManager中生成唯一的房间号传入，创建房间
        {
            RoomID = roomid;
            Player1 = player1;
            IsFull = false;//房间内还缺少一位玩家
            IfEnd = false;
            board = new Board();//棋盘初始化
        }

        public void AddPlayer(string player2)
        {
            Player2 = player2;
            IsFull = true;//第二位玩家加入房间，房间已满
        }

        public string DealPiece(int x,int y,string connectionId)
        {
            string color = "BLACK";
            if (connectionId == Player2) color = "WHITE";
            //判断落子合法性
            //判断是否有人胜利
            if (!GameRules.TestValid(x, y, color, board)) return "Invalid";

            string result = GameRules.TestEnd(x, y, color, board);
            if (result != "CONTINUE") IfEnd = true;
            return result;
        }
    }
}
