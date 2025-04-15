using ChessGame.Server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ChessGame.Server.Controllers
{
    public class RoomManager
    {
        private readonly List<Room> _rooms = new();//维护房间信息的链表
        private readonly IHubContext<GameHub> _hubContext;//允许在Hub类外执行与客户端的交互

        public RoomManager(IHubContext<GameHub> hubContext)
        {
            _hubContext = hubContext;
        }

        //房间匹配
        public async Task MatchPlayer(string connectionId)
        {
            // 查找是否有等待中的房间
            var waitingRoom = _rooms.FirstOrDefault(r => !r.IsFull);

            //判断当前请求玩家是否需要等待
            bool WaitPartner = (waitingRoom == null);//如果没有等待中的房间，则玩家需要等待
            await _hubContext.Clients.Client(connectionId)
                   .SendAsync("WaitingForOpponent", WaitPartner);

            if (waitingRoom != null)//加入玩家2
            {
                // 加入已有房间
                waitingRoom.AddPlayer(connectionId);

                await _hubContext.Groups.AddToGroupAsync(connectionId, waitingRoom.RoomID);

                // 通知双方配对成功
                await _hubContext.Clients.Client(waitingRoom.Player1)
                    .SendAsync("MatchSuccess", waitingRoom.RoomID, "你是黑方");
                await _hubContext.Clients.Client(waitingRoom.Player2)
                    .SendAsync("MatchSuccess", waitingRoom.RoomID, "你是白方");

            }
            else//加入玩家1
            {
                // 创建新房间
                var roomName = Guid.NewGuid().ToString();//为房间生成唯一的ID
                var newRoom = new Room(roomName, connectionId);
                _rooms.Add(newRoom);

                await _hubContext.Groups.AddToGroupAsync(connectionId, roomName);//将客户端加入指定的组，每个房间中的玩家在同一组
                /*await _hubContext.Clients.Client(connectionId)//向客户端发送等待匹配的消息
                    .SendAsync("WaitingForOpponent");*/
            }
        }


        //离开房间
        public async Task ExitRoom(string connectionId)
        {
            Room room = FindRoomByPlayer(connectionId);
            //删除房间
            _rooms.Remove(room);
        }

        // 根据 Player 查找房间
        public Room FindRoomByPlayer(string connectionId)
        {
            // 使用 List.Find 方法根据 RoomID 查找房间
            return _rooms.Find(room => 
            (room.Player1 == connectionId|| room.Player2 == connectionId));
        }

        //处理落子
        public async Task HandlePiece(string connectionId,int x,int y)
        {
            Room room = FindRoomByPlayer(connectionId);

            string result = room.DealPiece(x, y,connectionId);

            await _hubContext.Clients.Group(room.RoomID)
                   .SendAsync("PieceInfo", result);


            //如果有一方胜利
            if (result == "WIN")
            {
                if(connectionId == room.Player1)
                {
                    await _hubContext.Clients.Client(room.Player1)
                   .SendAsync("GameOver", "WIN");

                    await _hubContext.Clients.Client(room.Player2)
                   .SendAsync("GameOver", "LOSE");
                }
                else
                {
                    await _hubContext.Clients.Client(room.Player1)
                   .SendAsync("GameOver", "LOSE");

                    await _hubContext.Clients.Client(room.Player2)
                   .SendAsync("GameOver", "WIN");
                }
            }
            else if(result =="DRAW")//如果是和棋
            {
                await _hubContext.Clients.Client(room.Player1)
                   .SendAsync("GameOver", "DRAW");

                await _hubContext.Clients.Client(room.Player2)
               .SendAsync("GameOver", "DRAW");
            }
            else if(result == "CONTINUE")//如果对局没有结束
            {
                if (connectionId == room.Player1)
                {
                    await _hubContext.Clients.Client(room.Player1)
                   .SendAsync("State", "Wait");

                    await _hubContext.Clients.Client(room.Player2)
                   .SendAsync("State", "Act");
                }
                else
                {
                    await _hubContext.Clients.Client(room.Player1)
                   .SendAsync("State", "Act");

                    await _hubContext.Clients.Client(room.Player2)
                   .SendAsync("State", "Wait");
                }
            }
        }
    }
}
