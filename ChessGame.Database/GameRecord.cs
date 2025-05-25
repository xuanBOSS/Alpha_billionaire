
using System.ComponentModel.DataAnnotations;

namespace ChessGame.Database
{
    public class GameRecord
    {
        [Key]
        public string UserId { get; set; } // 对应数据库中的UserId字段
        public string UserName { get; set; } // 对应数据库中的UserName字段
        public int WinTimes { get; set; } // 对应数据库中的WinTimes字段
    }
}
