using System.Collections.Concurrent;

namespace ChessGame.Server.Services
{
    public class PlayerSessionManager
    {
        // 保存用户ID和连接ID的映射
        private readonly ConcurrentDictionary<string, string> _userConnections = new();
        // 保存连接ID和用户ID的映射
        private readonly ConcurrentDictionary<string, string> _connectionUsers = new();

        // 添加用户会话
        public void AddSession(string userId, string connectionId)
        {
            _userConnections.AddOrUpdate(userId, connectionId, (_, _) => connectionId);
            _connectionUsers.AddOrUpdate(connectionId, userId, (_, _) => userId);
        }

        // 移除用户会话
        public void RemoveSession(string connectionId)
        {
            if (_connectionUsers.TryRemove(connectionId, out string userId))
            {
                _userConnections.TryRemove(userId, out _);
            }
        }

        // 根据用户ID获取连接ID
        public string GetConnectionId(string userId)
        {
            _userConnections.TryGetValue(userId, out string connectionId);
            return connectionId;
        }

        // 根据连接ID获取用户ID
        public string GetUserId(string connectionId)
        {
            _connectionUsers.TryGetValue(connectionId, out string userId);
            return userId;
        }

        // 检查用户是否在线
        public bool IsUserOnline(string userId)
        {
            return _userConnections.ContainsKey(userId);
        }
    }
}