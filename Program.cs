using GlobalTradeSimulator.DataAccess;
using GlobalTradeSimulator.Web.Services; // <-- to find IGameEngine, GameEngine, IWarService, WarService

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options => {
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
});

// Register repositories
builder.Services.AddScoped<MarketRepository>();
builder.Services.AddScoped<PlayerRepository>();

// Register game services
builder.Services.AddScoped<IGameEngine, GameEngine>();
builder.Services.AddScoped<IWarService, WarService>();

builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Global Trade Simulator API V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapControllers();
app.MapHub<GameHub>("/gamehub");
app.Run();