using System.ComponentModel.DataAnnotations;
using UserTasksAndChat.Models;

namespace UserTasksAndChat.Dto.User
{
    public class UserUpdateDto
    {
        public string? FirstName { get; set; } = null;
        public string? LastName { get; set; } = null;

        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string? Email { get; set; } = null;

        public bool ResetPassword { get; set; } = false;
        public string? Password { get; set; } = null;

        public List<UserPermissions> Permissions { get; set; } = []; 
    }
}
