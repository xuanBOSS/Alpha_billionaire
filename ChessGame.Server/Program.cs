using ChessGame.Server.Hubs;
using ChessGame.Server.Controllers;
using ChessGame.Server.Services;
using ChessGame.Database;
using ChessGame.GameLogic;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ChessGame.AI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddDbContextFactory<ChessDbContext>(options =>
    options.UseMySql(
        "Server=localhost;Database=chessgamedb;User=root;Password=200498;",
        ServerVersion.AutoDetect("Server=localhost;Database=chessgamedb;User=root;Password=200498;")
    ));

// ȷ��SignalR������ȷ
builder.Services.AddSignalR()
    .AddHubOptions<GameHub>(options =>
    {
        options.EnableDetailedErrors = true;
    })
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = null;
        options.PayloadSerializerOptions.WriteIndented = true;
    });
// ������������ӿڷ���
builder.WebHost.UseUrls("http://0.0.0.0:5000;https://0.0.0.0:5001");
//builder.WebHost.UseUrls("http://localhost:5000");
// ע��PlayerSessionManager���񣨱�����RoomManager֮ǰע�ᣩ
builder.Services.AddSingleton<PlayerSessionManager>();

// �����Ҫ��ע��AI����
builder.Services.AddSingleton<AIService>();

builder.Services.AddSingleton<RoomManager>();

// Register AIHelper and GameManager
//builder.Services.AddSingleton<AIHelper>();
//builder.Services.AddSingleton<GameManager>();

// ע�� AIRoom ������
builder.Services.AddSingleton<AIRoomManager>();

// 3. ע��RoomManager����ʹ�ù������������ѭ������
// ������ע��RoomManager��ʹ��IDbContextFactory������ֱ��ʹ��DbContext
/*builder.Services.AddSingleton<RoomManager>(serviceProvider => {
    var hubContext = serviceProvider.GetRequiredService<IHubContext<GameHub>>();
    var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<ChessDbContext>>();
    var sessionManager = serviceProvider.GetRequiredService<PlayerSessionManager>();
    return new RoomManager(hubContext, dbContextFactory, sessionManager);
});*/

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

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.MapControllers();

app.MapRazorPages();

// ���� SignalR ·��
app.MapHub<GameHub>("/gamehub");

/*app.Run("https://localhost:7101");*/
app.Run();

