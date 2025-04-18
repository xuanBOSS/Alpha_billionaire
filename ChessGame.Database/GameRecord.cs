namespace ChessGame.Database
{
    public class GameRecord
    {
        public string Id { get; set; }//玩家Id
        public int VictoryTimes { get; set; }//胜利次数
        public int FailureTimes { get; set; }//失败次数
    }
}