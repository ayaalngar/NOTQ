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
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=notq.db";

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString, b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        services.Configure<AudioStorageOptions>(configuration.GetSection(AudioStorageOptions.SectionName));
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));

        // Auth infrastructure unwired for child-root account model:
        // services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        // services.AddSingleton<IPasswordHasher, PasswordHasher>();
        // services.AddScoped<IJwtTokenService, JwtTokenService>();
        // services.AddAuthentication(...).AddJwtBearer(...);

        services.AddScoped<IAudioStorageService, LocalAudioStorageService>();

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

        services.AddScoped<IScoringService, ScoringService>();

        services.Configure<RailwayAnalysisOptions>(configuration.GetSection(RailwayAnalysisOptions.SectionName));
        services.AddHttpClient(PronunciationAnalysisApiClient.ClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RailwayAnalysisOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddScoped<PronunciationAnalysisApiClient>();
        services.AddScoped<IPronunciationAnalysisService, RailwayPronunciationAnalysisService>();
        services.AddScoped<ISessionScreeningService, RailwaySessionScreeningService>();

        services.Configure<GradioOptions>(options =>
        {
            configuration.GetSection(GradioOptions.SectionName).Bind(options);
            if (string.IsNullOrWhiteSpace(options.ApiToken))
            {
                options.ApiToken = configuration["HF_TOKEN"] ?? Environment.GetEnvironmentVariable("HF_TOKEN");
            }
        });
        services.AddHttpClient(GradioWordVerificationService.ClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GradioOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            if (!string.IsNullOrWhiteSpace(options.ApiToken))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiToken);
            }
        });
        services.AddScoped<IWordVerificationService, GradioWordVerificationService>();

        return services;
    }
}
