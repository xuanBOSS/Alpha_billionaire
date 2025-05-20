using ChessGame.Server.Hubs;
using ChessGame.Server.Controllers;
using ChessGame.Server.Services;
using ChessGame.Database;
using ChessGame.GameLogic;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 添加数据库服务
builder.Services.AddDbContext<ChessDbContext>(options =>
    options.UseMySql("Server=localhost;Database=chessgamedb;User=root;Password=200498;",
        new MySqlServerVersion(new Version(8, 0, 21))));

// 添加 SignalR 服务
builder.Services.AddSignalR();

// 允许所有网络接口访问
builder.WebHost.UseUrls("http://0.0.0.0:5000;https://0.0.0.0:5001");

// 注册PlayerSessionManager服务（必须在RoomManager之前注册）
builder.Services.AddSingleton<PlayerSessionManager>();

// 如果需要，注册AI服务
builder.Services.AddSingleton<AIService>();

// 3. 注册RoomManager，但使用工厂方法来解决循环依赖
builder.Services.AddSingleton<RoomManager>(serviceProvider => {
    var hubContext = serviceProvider.GetRequiredService<Microsoft.AspNetCore.SignalR.IHubContext<GameHub>>();
    var dbContext = serviceProvider.GetRequiredService<ChessDbContext>();
    var sessionManager = serviceProvider.GetRequiredService<PlayerSessionManager>();
    return new RoomManager(hubContext, dbContext, sessionManager);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.MapControllers();

app.MapRazorPages();

// 配置 SignalR 路由
app.MapHub<GameHub>("/gamehub");

/*app.Run("https://localhost:7101");*/
app.Run();

