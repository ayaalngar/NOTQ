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

        // services.AddScoped<IAuthService, AuthService>(); (Unwired for child-root account model)
        services.AddScoped<IMobileChildService, MobileChildService>();
        services.AddScoped<IAssessmentService, AssessmentService>();
        services.AddScoped<IPracticeService, PracticeService>();
        services.AddScoped<IAttemptProcessingService, AttemptProcessingService>();
        services.AddScoped<IHomeService, HomeService>();

        return services;
    }
}
