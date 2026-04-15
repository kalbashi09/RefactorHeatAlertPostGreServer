using RefactorHeatAlertPostGre.Data;
using RefactorHeatAlertPostGre.Infrastructure.Middleware;
using Microsoft.EntityFrameworkCore;
using RefactorHeatAlertPostGre.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://heatsync-zs03.onrender.com", "http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register all our custom services
builder.Services.AddHeatAlertServices(builder.Configuration);

var app = builder.Build();

// Configure pipeline
// if (app.Environment.IsDevelopment())
// {
    app.UseSwagger();
    app.UseSwaggerUI();
// 

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

// Optional: global API key middleware (can be used instead of attribute checks)
// app.UseMiddleware<ApiKeyMiddleware>();

app.UseAuthorization();
app.MapControllers();

// Auto-migrate and seed on startup (development convenience)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    await DbInitializer.InitializeAsync(dbContext);
}

// Start the Telegram bot (singleton service, so we get it and start)
var botService = app.Services.GetRequiredService<ITelegramBotService>();
botService.StartReceiving();

app.Run();