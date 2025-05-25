using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using System.Windows.Controls;


namespace ChessGame.Database
{
    public class ChessDbContext : DbContext
    {
        public DbSet<Player> Players { get; set; } // 玩家账号信息
        public DbSet<GameRecord> GameRecords { get; set; } // 玩家的对局排行榜

        public ChessDbContext(DbContextOptions<ChessDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 映射到我们新建的表
            modelBuilder.Entity<Player>().ToTable("users");
            modelBuilder.Entity<GameRecord>().ToTable("gamerecords");

            // 设置主键
            modelBuilder.Entity<Player>().HasKey(p => p.UserId);
            modelBuilder.Entity<GameRecord>().HasKey(g => g.UserId);
        }
    }

    // 设计时工厂类，用于创建 DbContext 实例
    public class ChessDbContextFactory : IDesignTimeDbContextFactory<ChessDbContext>
    {
        public ChessDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ChessDbContext>();

            // 配置数据库连接字符串 (更新为我们新建的数据库)
            var connectionString = "Server=localhost;Database=chessgamedb;User=root;Password=200498;";
            optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)));

            return new ChessDbContext(optionsBuilder.Options);
        }
    }
}
