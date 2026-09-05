using Microsoft.EntityFrameworkCore;
using NOTQ.API.Extensions;
using NOTQ.API.Middleware;
using NOTQ.API.Services;
using NOTQ.Application;
using NOTQ.Application.Common.Interfaces;
using NOTQ.Infrastructure;
using NOTQ.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Application & Infrastructure Services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 2. Web & HTTP Context
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// 3. Controllers & JSON Configuration
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// 4. CORS Policy (Enabling parallel Flutter web & mobile development)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 5. OpenAPI / Swagger Documentation
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

// 6. Automatic Database Migration & Seeding in Development
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

// 7. HTTP Request Pipeline
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.UseCors("AllowAll");

app.UseStaticFiles();

// Swagger enabled in all environments for API contracts testing
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "NOTQ Backend API v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
