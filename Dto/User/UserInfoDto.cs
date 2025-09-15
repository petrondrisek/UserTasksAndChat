using UserTasksAndChat.Models;
using System.ComponentModel.DataAnnotations;

namespace UserTasksAndChat.Dto.User
{
    public class UserInfoDto
    {
        [Required(ErrorMessage = "User ID is required.")]
        public Guid Id { get; set; }
        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; } = string.Empty;
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = false;
        public IList<UserPermissions> Permissions { get; set; } = new List<UserPermissions> { UserPermissions.User };
    }
}
