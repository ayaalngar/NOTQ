using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NOTQ.Application.Common.Interfaces;
using NOTQ.Application.Interfaces;
using NOTQ.Infrastructure.AI;
using NOTQ.Infrastructure.Authentication;
using NOTQ.Infrastructure.Persistence;
using NOTQ.Infrastructure.Services;
using NOTQ.Infrastructure.Storage;

namespace NOTQ.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Database Persistence
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=NOTQDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // 2. Options Configuration
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AudioStorageOptions>(configuration.GetSection(AudioStorageOptions.SectionName));
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));

        // 3. Security & Authentication
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var key = Encoding.UTF8.GetBytes(jwtOptions.SecretKey);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        // 4. Storage Service
        services.AddScoped<IAudioStorageService, LocalAudioStorageService>();

        // 5. AI Speech Analysis Integration (Parallel workflow toggle)
        var aiSection = configuration.GetSection(AiOptions.SectionName);
        var useMock = aiSection.GetValue<bool?>("UseMock") ?? true;

        if (useMock)
        {
            services.AddScoped<ISpeechAnalysisService, MockSpeechAnalysisService>();
        }
        else
        {
            services.AddHttpClient<ISpeechAnalysisService, AiSpeechAnalysisService>();
        }

        // 6. Domain/Infrastructure Scoring & Reports
        services.AddScoped<IScoringService, ScoringService>();
        services.AddScoped<IPatternDetectionService, PatternDetectionService>();
        services.AddScoped<IProgressService, ProgressService>();
        services.AddScoped<IReportService, ReportService>();

        return services;
    }
}
