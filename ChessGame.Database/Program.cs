using Microsoft.EntityFrameworkCore;
using ChessGame.Database;
using System.Windows.Controls;

var builder = WebApplication.CreateBuilder(args);

// 添加 DbContext 服务
builder.Services.AddDbContext<ChessDbContext>(options =>
    options.UseMySql("Server=localhost;Database=ChessDb;User=root;Password=xu2365651;",
        new MySqlServerVersion(new Version(8, 0, 21))));

var app = builder.Build();

// 添加一个新用户
app.MapPost("/players", async (Player player, ChessDbContext db) =>
{
    db.Players.Add(player);
    await db.SaveChangesAsync();
    return Results.Created($"/players/{player.Id}", player);
});

// 添加一条新的对局记录
app.MapPost("/gamerecords", async (GameRecord gamerecord, ChessDbContext db) =>
{
    db.GameRecords.Add(gamerecord);
    await db.SaveChangesAsync();
    return Results.Created($"/gamerecords/{gamerecord.Id}", gamerecord);
});

// 根据 ID 获取用户信息
app.MapGet("/players/{id}", async (string id, ChessDbContext db) =>
    await db.Players.FindAsync(id) is Player player
        ? Results.Ok(player)
        : Results.NotFound());

// 根据 ID 获取用户对局记录
app.MapGet("/gamerecords/{id}", async (string id, ChessDbContext db) =>
    await db.GameRecords.FindAsync(id) is GameRecord gamerecord
        ? Results.Ok(gamerecord)
        : Results.NotFound());

// 根据Id删除用户
app.MapDelete("/players/{id}", async (string id, ChessDbContext db) =>
{
    var player = await db.Players.FindAsync(id);
    if (player is null) return Results.NotFound();

    db.Players.Remove(player);
    await db.SaveChangesAsync();
    return Results.Ok();
});

// 根据Id删除对局记录
app.MapDelete("/gamerecords/{id}", async (string id, ChessDbContext db) =>
{
    var gamerecord = await db.GameRecords.FindAsync(id);
    if (gamerecord is null) return Results.NotFound();

    db.GameRecords.Remove(gamerecord);
    await db.SaveChangesAsync();
    return Results.Ok();
});

app.MapGet("/", () => "Hello World!");

app.Run();
