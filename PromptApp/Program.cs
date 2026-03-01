using PromptApp.Data;
using PromptApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PromptAppDbContext>(options =>
    options.UseSqlite("Data Source=/app/data/prompts.db"));

builder.Services.AddScoped<PromptService>();
builder.Services.AddSingleton<RabbitMqPublisher>(sp =>
{
    return RabbitMqPublisher.CreateAsync().GetAwaiter().GetResult();
});

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PromptApp API",
        Version = "v1"
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PromptApp API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseRouting();

app.UseCors("ReactPolicy");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
