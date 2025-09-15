using System.ComponentModel.DataAnnotations;

namespace UserTasksAndChat.Dto.User
{
    public class UserRegisterDto
    {
        [Required]
        [StringLength(32, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 32 characters long.")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Username can only contain letters, numbers, and underscores.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; } = string.Empty;
    }
}
