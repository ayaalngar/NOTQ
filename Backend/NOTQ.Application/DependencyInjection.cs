using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NOTQ.Application.Interfaces;
using NOTQ.Application.Services;

namespace NOTQ.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IChildService, ChildService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IAttemptService, AttemptService>();
        services.AddScoped<IPracticeWordService, PracticeWordService>();

        return services;
    }
}
