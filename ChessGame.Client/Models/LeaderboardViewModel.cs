using System.Collections.ObjectModel;

namespace ChessGame.Client.Models
{
    public class LeaderboardViewModel
    {
        public ObservableCollection<PlayerRankData> Players { get; set; } = new ObservableCollection<PlayerRankData>();
    }

    public class PlayerRankData
    {
        public string Name { get; set; }
        public int Score { get; set; }
        public int Rank { get; set; }
    }
}