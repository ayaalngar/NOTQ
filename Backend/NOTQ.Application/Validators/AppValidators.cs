using FluentValidation;
using NOTQ.Application.DTOs.Attempts;
using NOTQ.Application.DTOs.Auth;
using NOTQ.Application.DTOs.Children;
using NOTQ.Application.DTOs.Sessions;

namespace NOTQ.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}

public class CreateChildValidator : AbstractValidator<CreateChildDto>
{
    public CreateChildValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Child name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .LessThan(DateTime.UtcNow).WithMessage("Date of birth must be in the past.")
            .Must(dob => DateTime.UtcNow.Year - dob.Year <= 18)
            .WithMessage("Child age must be 18 years or younger.");
    }
}

public class UpdateChildValidator : AbstractValidator<UpdateChildDto>
{
    public UpdateChildValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Child name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .LessThan(DateTime.UtcNow).WithMessage("Date of birth must be in the past.");
    }
}

public class StartSessionValidator : AbstractValidator<StartSessionDto>
{
    public StartSessionValidator()
    {
        RuleFor(x => x.ChildId)
            .NotEmpty().WithMessage("ChildId is required.");
    }
}

public class SubmitAttemptValidator : AbstractValidator<SubmitAttemptRequestDto>
{
    private static readonly string[] AllowedExtensions = { ".wav", ".mp3", ".m4a", ".aac", ".ogg", ".webm" };
    private const long MaxFileSizeBytes = 15 * 1024 * 1024; // 15MB

    public SubmitAttemptValidator()
    {
        RuleFor(x => x.WordId)
            .GreaterThan(0).WithMessage("Valid WordId is required.");

        RuleFor(x => x.Audio)
            .NotNull().WithMessage("Audio file is required.")
            .Must(file => file != null && file.Length > 0).WithMessage("Audio file cannot be empty.")
            .Must(file => file != null && file.Length <= MaxFileSizeBytes).WithMessage("Audio file size must not exceed 15MB.")
            .Must(file =>
            {
                if (file == null) return false;
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                return AllowedExtensions.Contains(ext);
            }).WithMessage("Audio format must be one of: .wav, .mp3, .m4a, .aac, .ogg, .webm.");
    }
}
