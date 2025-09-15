using System.ComponentModel.DataAnnotations;

namespace UserTasksAndChat.Dto.Token
{
    public class UserRefreshTokenGenerateDto
    {
        [Required]
        public string Token = string.Empty;

        [Required]
        public int ValidUntilHours = 0;
    }
}
