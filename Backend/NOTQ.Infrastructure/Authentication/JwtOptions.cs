namespace NOTQ.Infrastructure.Authentication;

public class JwtOptions
{
    public const string SectionName = "JwtSettings";

    public string SecretKey { get; set; } = "NOTQ_SUPER_SECURE_JWT_SECRET_KEY_FOR_SCREENING_MVP_2026_CHANGEME!";
    public string Issuer { get; set; } = "NOTQ.API";
    public string Audience { get; set; } = "NOTQ.Client";
    public int AccessTokenExpiryMinutes { get; set; } = 60;
    public int RefreshTokenExpiryDays { get; set; } = 30;
}
