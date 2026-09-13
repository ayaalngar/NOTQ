using Microsoft.OpenApi;

namespace NOTQ.API.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "NOTQ Backend API",
                Version = "v1",
                Description = "Child-friendly speech-pronunciation screening platform API. Orchestrates Flutter client, SQL Server persistence, and AI inference.",
                Contact = new OpenApiContact
                {
                    Name = "NOTQ Team"
                }
            });

        });

        return services;
    }
}
