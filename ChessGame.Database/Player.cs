//namespace ChessGame.Database
//{
//    public class Player
//    {
//        public string Id { get; set; }//用户名
//        public string Password { get; set; }//密码
//    }
//}
using System.ComponentModel.DataAnnotations;

namespace ChessGame.Database
{
    public class Player
    {
        [Key]
        public string UserId { get; set; } // 对应数据库中的UserId字段
        public string PassWord { get; set; } // 对应数据库中的PassWord字段
        public string UserName { get; set; } // 对应数据库中的UserName字段
    }
}
