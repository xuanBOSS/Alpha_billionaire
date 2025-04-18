using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using System.Windows.Controls;

namespace ChessGame.Database
{
    public class ChessDbContext : DbContext
    {
        public DbSet<Player> Players { get; set; }//玩家账号信息
        public DbSet<GameRecord> GameRecords { get; set; }//玩家的对局排行榜

        public ChessDbContext(DbContextOptions<ChessDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)//创建数据表
        {
            modelBuilder.Entity<Player>().ToTable("Players");
            modelBuilder.Entity<GameRecord>().ToTable("GameRecords");
        }
    }

    // 设计时工厂类，用于创建 DbContext 实例
    public class ChessDbContextFactory : IDesignTimeDbContextFactory<ChessDbContext>
    {
        public ChessDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ChessDbContext>();

            // 配置数据库连接字符串
            var connectionString = "Server=localhost;Database=ChessDb;User=root;Password=xu2365651;";
            optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)));

            return new ChessDbContext(optionsBuilder.Options);
        }
    }
}