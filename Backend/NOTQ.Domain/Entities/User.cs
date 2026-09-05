using NOTQ.Domain.Common;
using NOTQ.Domain.Enums;

namespace NOTQ.Domain.Entities;

public class User : BaseEntity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Parent;

    public ICollection<Child> Children { get; set; } = new List<Child>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
