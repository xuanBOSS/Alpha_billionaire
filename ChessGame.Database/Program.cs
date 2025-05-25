using Microsoft.EntityFrameworkCore;
using ChessGame.Database;
using System.Windows.Controls;
using Microsoft.AspNetCore.Mvc;


var builder = WebApplication.CreateBuilder(args);

// 添加 DbContext 服务
builder.Services.AddDbContext<ChessDbContext>(options =>
    options.UseMySql("Server=localhost;Database=chessgamedb;User=root;Password=200498;",
        new MySqlServerVersion(new Version(8, 0, 21))));

// 添加控制器服务
builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// 用户注册
app.MapPost("/register", async (Player player, ChessDbContext db) =>
{
    // 检查用户是否已存在
    var existingPlayer = await db.Players.FindAsync(player.UserId);
    if (existingPlayer != null)
    {
        return Results.BadRequest("用户ID已存在");
    }

    // 添加新用户
    db.Players.Add(player);
    await db.SaveChangesAsync();

    // 添加默认游戏记录
    var gameRecord = new GameRecord
    {
        UserId = player.UserId,
        UserName = player.UserName,
        WinTimes = 0
    };
    db.GameRecords.Add(gameRecord);
    await db.SaveChangesAsync();

    return Results.Created($"/players/{player.UserId}", player);
});

// 用户登录
app.MapPost("/login", async ([FromBody] LoginRequest request, ChessDbContext db) =>
{
    var player = await db.Players.SingleOrDefaultAsync(p => p.UserId == request.UserId);

    if (player == null || player.PassWord != request.Password)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        UserId = player.UserId,
        UserName = player.UserName
    });
});

// 获取排行榜
app.MapGet("/leaderboard", async (ChessDbContext db) =>
{
    var leaderboard = await db.GameRecords
        .OrderByDescending(g => g.WinTimes)
        .Select(g => new
        {
            g.UserId,
            g.UserName,
            g.WinTimes,
            Rank = 0 // 将在下方计算排名
        })
        .ToListAsync();

    // 计算排名
    int rank = 1;
    int lastScore = -1;
    int lastRank = 0;

    for (int i = 0; i < leaderboard.Count; i++)
    {
        var record = leaderboard[i];
        var currentScore = record.WinTimes;

        if (currentScore != lastScore)
        {
            lastRank = rank;
            lastScore = currentScore;
        }

        // 动态创建新对象设置排名（因为原对象可能是不可变的）
        leaderboard[i] = new
        {
            record.UserId,
            record.UserName,
            record.WinTimes,
            Rank = lastRank
        };

        rank++;
    }

    return Results.Ok(leaderboard);
});

// 更新胜利次数
app.MapPut("/player/win/{userId}", async (string userId, ChessDbContext db) =>
{
    var gameRecord = await db.GameRecords.FindAsync(userId);
    if (gameRecord == null)
    {
        return Results.NotFound();
    }

    gameRecord.WinTimes += 1;
    await db.SaveChangesAsync();

    return Results.Ok();
});

// 获取用户信息
app.MapGet("/player/{userId}", async (string userId, ChessDbContext db) =>
{
    var player = await db.Players.FindAsync(userId);
    if (player == null)
    {
        return Results.NotFound();
    }

    var gameRecord = await db.GameRecords.FindAsync(userId);

    return Results.Ok(new
    {
        UserId = player.UserId,
        UserName = player.UserName,
        WinTimes = gameRecord?.WinTimes ?? 0
    });
});

app.Run();

// 登录请求模型
public class LoginRequest
{
    public string UserId { get; set; }
    public string Password { get; set; }
}
