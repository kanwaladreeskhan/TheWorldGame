using GlobalTradeSimulator.DataAccess; // Ensure this matches your namespace

var builder = WebApplication.CreateBuilder(args);

// 1. Add services to the container
builder.Services.AddControllers().AddJsonOptions(options => {
    options.JsonSerializerOptions.PropertyNamingPolicy = null; // PascalCase enable
});

// 2. Register Repositories (Dependency Injection)
// Ye lazmi hai taake Controllers database data fetch kar sakein
builder.Services.AddScoped<MarketRepository>();
builder.Services.AddScoped<PlayerRepository>(); 
// Agar aur repositories hain (e.g. InventoryRepository), toh unhein bhi yahan add karein

builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IGameEngine, GameEngine>();
builder.Services.AddScoped<IWarService, WarService>();

var app = builder.Build();

// 3. Configure the HTTP request pipeline
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
app.UseRouting(); // Routing enable karein
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapControllers();

app.Run();