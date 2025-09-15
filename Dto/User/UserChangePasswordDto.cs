using System.ComponentModel.DataAnnotations;

namespace UserTasksAndChat.Dto.User
{
    public class UserChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).+$",
        ErrorMessage = "Password must contain at least one uppercase letter, one number, and one special character.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
