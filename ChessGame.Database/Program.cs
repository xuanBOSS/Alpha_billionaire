using Microsoft.EntityFrameworkCore;
using ChessGame.Database;
using System.Windows.Controls;
using Microsoft.AspNetCore.Mvc;


var builder = WebApplication.CreateBuilder(args);

// ��� DbContext ����
builder.Services.AddDbContext<ChessDbContext>(options =>
    options.UseMySql("Server=localhost;Database=chessgamedb;User=root;Password=200498;",
        new MySqlServerVersion(new Version(8, 0, 21))));

// ��ӿ���������
builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// �û�ע��
app.MapPost("/register", async (Player player, ChessDbContext db) =>
{
    // ����û��Ƿ��Ѵ���
    var existingPlayer = await db.Players.FindAsync(player.UserId);
    if (existingPlayer != null)
    {
        return Results.BadRequest("�û�ID�Ѵ���");
    }

    // ������û�
    db.Players.Add(player);
    await db.SaveChangesAsync();

    // ���Ĭ����Ϸ��¼
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

// �û���¼
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

// ��ȡ���а�
app.MapGet("/leaderboard", async (ChessDbContext db) =>
{
    var leaderboard = await db.GameRecords
        .OrderByDescending(g => g.WinTimes)
        .Select(g => new
        {
            g.UserId,
            g.UserName,
            g.WinTimes,
            Rank = 0 // �����·���������
        })
        .ToListAsync();

    // ��������
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

        // ��̬�����¶���������������Ϊԭ��������ǲ��ɱ�ģ�
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

// ����ʤ������
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

// ��ȡ�û���Ϣ
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

// ��¼����ģ��
public class LoginRequest
{
    public string UserId { get; set; }
    public string Password { get; set; }
}
