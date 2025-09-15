using Microsoft.EntityFrameworkCore;

namespace UserTasksAndChat.Models
{
    [Index(nameof(Username), IsUnique = true)]
    [Index(nameof(Email), IsUnique = true)]
    public class User: Entity
    {
        public Guid Id { get; set; }
        public required string Username { get; set; }
        public string? FirstName { get; set; } = null;
        public string? LastName { get; set; } = null;
        public required string Email { get; set; }
        public string? Password { get; set; } = null;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public IList<UserPermissions> Permissions { get; set; } = new List<UserPermissions> { UserPermissions.User };
    }

    public enum UserPermissions
    {
        User,
        ManageUsers,
        ManageMissions
    }
}
