using Microsoft.EntityFrameworkCore;
using NOTQ.API.Extensions;
using NOTQ.API.Middleware;
using NOTQ.API.Services;
using NOTQ.Application;
using NOTQ.Application.Common.Interfaces;
using NOTQ.Infrastructure;
using NOTQ.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Applying database migrations...");
        db.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.UseCors("AllowAll");

app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "NOTQ Backend API v1");
    c.RoutePrefix = "swagger";
});

// app.UseAuthentication(); (Unwired for child-root account model)
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
