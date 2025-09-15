using System.ComponentModel.DataAnnotations;

namespace UserTasksAndChat.Dto.User
{
    public class UserLoginDto
    {
        [Required]
        [StringLength(32, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 32 characters long.")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Username can only contain letters, numbers, and underscores.")]
        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}
